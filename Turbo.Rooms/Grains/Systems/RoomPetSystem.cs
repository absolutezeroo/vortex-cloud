using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Database.Entities.Furniture;
using Turbo.Database.Entities.Pets;
using Turbo.Logging;
using Turbo.Primitives;
using Turbo.Primitives.Action;
using Turbo.Primitives.Messages.Outgoing.Inventory.Pets;
using Turbo.Primitives.Messages.Outgoing.Notifications;
using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Messages.Outgoing.Room.Pets;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Pets.Snapshots;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Snapshots.Avatars;

namespace Turbo.Rooms.Grains.Systems;

public sealed class RoomPetSystem(RoomGrain roomGrain)
{
    private const int PetPlacementForbiddenInFlatError = 1;
    private const int PetPlacementSelectedTileNotFreeError = 4;
    private const int MonsterplantPetType = 16;
    private readonly Dictionary<int, PendingBreedingSession> _breedingByPetOneId = [];
    private readonly Dictionary<int, PetMotionState> _motionByPetId = [];

    private readonly RoomGrain _roomGrain = roomGrain;
    private long _nextPetFlushAtMs = -1;

    public async Task EnsurePetsLoadedAsync(CancellationToken ct)
    {
        if (_roomGrain._state.IsPetsLoaded)
        {
            return;
        }

        await using TurboDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PetEntity[] pets = await dbCtx
            .Pets.AsNoTracking()
            .Where(p => p.RoomEntityId == _roomGrain.RoomId.Value && p.DeletedAt == null)
            .ToArrayAsync(ct)
            .ConfigureAwait(false);

        _roomGrain._state.PetsById.Clear();

        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        foreach (PetEntity pet in pets)
        {
            ApplyOfflineDecay(pet, nowMs);
            _roomGrain._state.PetsById[pet.Id] = RoomPetRuntime.ToSnapshot(pet);
        }

        _roomGrain._state.IsPetsLoaded = true;
    }

    public async Task ProcessPetsAsync(long now, CancellationToken ct)
    {
        if (now < _roomGrain._state.NextPetBoundaryMs)
        {
            return;
        }

        while (now >= _roomGrain._state.NextPetBoundaryMs)
        {
            _roomGrain._state.NextPetBoundaryMs += _roomGrain._roomConfig.Pet.TickMs;
        }

        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        if (_roomGrain._state.PetsById.Count == 0)
        {
            return;
        }

        await EnsureRoomReadyForPetPlacementAsync(ct).ConfigureAwait(false);

        List<RoomAvatarSnapshot> dirtySnapshots = [];

        foreach (
            PetSnapshot pet in _roomGrain._state.PetsById.Values.OrderBy(p => p.PetId).ToArray()
        )
        {
            try
            {
                PetMotionState motion = GetMotionState(pet, now);
                PetSnapshot current = ApplyPendingPetStep(pet, motion);

                if (pet.Type == MonsterplantPetType)
                {
                    continue;
                }

                current = ApplyNeedDecay(current, motion, now);

                if (motion.PendingSleepVocal)
                {
                    motion.PendingSleepVocal = false;
                    motion.NextVocalAtMs = ScheduleNextVocalAt(now);
                    await BroadcastPetVocalAsync(current, "SLEEPING").ConfigureAwait(false);
                }
                else if (motion.PendingWakeVocal)
                {
                    motion.PendingWakeVocal = false;
                    motion.NextVocalAtMs = ScheduleNextVocalAt(now);
                    await BroadcastPetVocalAsync(current, "GENERIC_HAPPY").ConfigureAwait(false);
                }
                else if (motion.NextVocalAtMs < 0)
                {
                    motion.NextVocalAtMs = ScheduleNextVocalAt(now);
                }
                else if (now >= motion.NextVocalAtMs)
                {
                    motion.NextVocalAtMs = ScheduleNextVocalAt(now);
                    await BroadcastPetVocalAsync(current, SelectVocalForState(current, motion))
                        .ConfigureAwait(false);
                }

                if (motion.IsSleeping && !motion.SleepPostureSent)
                {
                    motion.SleepPostureSent = true;
                    RoomPetAvatarSnapshot sleepSnapshot = await ToAvatarSnapshotAsync(
                            current,
                            "/lay/",
                            ct
                        )
                        .ConfigureAwait(false);
                    dirtySnapshots.Add(sleepSnapshot);
                }
                else if (!motion.IsSleeping)
                {
                    RoomPetAvatarSnapshot? update = await ProcessPetMotionAsync(
                            current,
                            motion,
                            now,
                            ct
                        )
                        .ConfigureAwait(false);

                    if (update is not null)
                    {
                        dirtySnapshots.Add(update);
                    }
                }
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogError(
                    ex,
                    "Failed to process pet movement tick for pet {PetId} in room {RoomId}",
                    pet.PetId,
                    _roomGrain.RoomId
                );
            }
        }

        if (dirtySnapshots.Count > 0)
        {
            await _roomGrain
                .SendComposerToRoomAsync(
                    new UserUpdateMessageComposer { Avatars = dirtySnapshots.ToImmutableArray() }
                )
                .ConfigureAwait(false);
        }

        if (_nextPetFlushAtMs < 0)
        {
            _nextPetFlushAtMs = now + _roomGrain._roomConfig.Pet.StatFlushIntervalMs;
        }
        else if (now >= _nextPetFlushAtMs)
        {
            _nextPetFlushAtMs = now + _roomGrain._roomConfig.Pet.StatFlushIntervalMs;

            try
            {
                await FlushDirtyPetsAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogError(
                    ex,
                    "Failed to flush dirty pet stats in room {RoomId}",
                    _roomGrain.RoomId
                );
            }
        }
    }

    public async Task<PetSnapshot?> PlacePetAsync(
        ActionContext ctx,
        int petId,
        int x,
        int y,
        Rotation direction,
        CancellationToken ct
    )
    {
        if (
            !_roomGrain._state.RoomSnapshot.AllowPets
            && _roomGrain._state.RoomSnapshot.OwnerId != ctx.PlayerId
        )
        {
            await SendPetPlacingErrorAsync(ctx, PetPlacementForbiddenInFlatError)
                .ConfigureAwait(false);
            return null;
        }

        await EnsureRoomReadyForPetPlacementAsync(ct).ConfigureAwait(false);

        double z;

        try
        {
            z = GetTileHeightForPet(x, y);
        }
        catch (TurboException ex)
            when (ex.ErrorCode
                    is TurboErrorCodeEnum.TileOutOfBounds
                        or TurboErrorCodeEnum.InvalidMoveTarget
            )
        {
            await SendPetPlacingErrorAsync(ctx, PetPlacementSelectedTileNotFreeError)
                .ConfigureAwait(false);
            return null;
        }

        await using TurboDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PetEntity? pet = await dbCtx
            .Pets.SingleOrDefaultAsync(
                p =>
                    p.Id == petId
                    && p.OwnerPlayerEntityId == ctx.PlayerId.Value
                    && p.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (pet is null)
        {
            _roomGrain._logger.LogDebug(
                "Pet placement ignored because pet {PetId} was not found for player {PlayerId}",
                petId,
                ctx.PlayerId
            );
            return null;
        }

        if (pet.RoomEntityId is not null && pet.RoomEntityId != _roomGrain.RoomId.Value)
        {
            _roomGrain._logger.LogDebug(
                "Pet placement ignored because pet {PetId} is already in room {ExistingRoomId}",
                petId,
                pet.RoomEntityId
            );
            return null;
        }

        bool wasInInventory = pet.RoomEntityId is null;

        if (wasInInventory)
        {
            ApplyOfflineDecay(pet, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        pet.RoomEntityId = _roomGrain.RoomId.Value;
        pet.X = x;
        pet.Y = y;
        pet.Z = z;
        pet.Direction = (int)direction;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        PetSnapshot snapshot = RoomPetRuntime.ToSnapshot(pet);
        _roomGrain._state.PetsById[pet.Id] = snapshot;

        if (wasInInventory)
        {
            await SendPetRemovedFromInventoryAsync(snapshot).ConfigureAwait(false);
        }

        await SendPetAddedAsync(snapshot, ct).ConfigureAwait(false);

        return snapshot;
    }

    public async Task<PetSnapshot?> MovePetAsync(
        ActionContext ctx,
        int petId,
        int x,
        int y,
        Rotation direction,
        CancellationToken ct
    )
    {
        await EnsureRoomReadyForPetPlacementAsync(ct).ConfigureAwait(false);

        double z = GetTileHeightForPet(x, y);

        await using TurboDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PetEntity? pet = await LoadPlacedPetAsync(dbCtx, petId, ct).ConfigureAwait(false);

        if (pet is null)
        {
            return null;
        }

        EnsurePetOwner(ctx, pet);

        pet.X = x;
        pet.Y = y;
        pet.Z = z;
        pet.Direction = (int)direction;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        PetSnapshot snapshot = RoomPetRuntime.ToSnapshot(pet);

        // Preserve live in-memory stats (may not have been flushed to DB yet)
        if (_roomGrain._state.PetsById.TryGetValue(pet.Id, out PetSnapshot? prev))
        {
            snapshot = snapshot with
            {
                Nutrition = prev.Nutrition,
                Energy = prev.Energy,
                Experience = prev.Experience,
                Level = prev.Level,
                Respect = prev.Respect,
            };
        }

        _roomGrain._state.PetsById[pet.Id] = snapshot;

        await SendPetUpdatedAsync(snapshot, ct).ConfigureAwait(false);

        return snapshot;
    }

    public async Task<PetSnapshot?> PickUpPetAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PetEntity? pet = await LoadPlacedPetAsync(dbCtx, petId, ct).ConfigureAwait(false);

        if (pet is null)
        {
            return null;
        }

        EnsurePetOwner(ctx, pet);

        // Sync live in-memory stats so pickup persists the latest values
        if (_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? liveSnapshot))
        {
            pet.Nutrition = liveSnapshot.Nutrition;
            pet.Energy = liveSnapshot.Energy;
            pet.Experience = liveSnapshot.Experience;
            pet.Level = liveSnapshot.Level;
            pet.Respect = liveSnapshot.Respect;
        }

        pet.RoomEntityId = null;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        _roomGrain._state.PetsById.Remove(pet.Id);
        _motionByPetId.Remove(petId);

        PetSnapshot snapshot = RoomPetRuntime.ToSnapshot(pet);

        await _roomGrain
            .SendComposerToRoomAsync(
                new UserRemoveMessageComposer { ObjectId = RoomPetRuntime.ToRoomObjectId(pet.Id) }
            )
            .ConfigureAwait(false);

        await SendPetAddedToInventoryAsync(snapshot, ct).ConfigureAwait(false);

        return snapshot;
    }

    public async Task<PetFeedResult> FeedPetAsync(
        ActionContext ctx,
        int petId,
        RoomObjectId foodItemId,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        // Sync live in-memory stats into this context so the feed operates on the correct base
        await SyncLiveStatsToPetEntityAsync(dbCtx, petId, ct).ConfigureAwait(false);

        PetFeedResult result = await RoomPetRuntime
            .FeedAsync(
                dbCtx,
                _roomGrain.RoomId.Value,
                ctx.PlayerId.Value,
                petId,
                foodItemId,
                _roomGrain._state.RoomSnapshot.AllowPetsEat,
                _roomGrain._roomConfig.Pet.NutritionCap,
                _roomGrain._roomConfig.Pet.EnergyCap,
                ct
            )
            .ConfigureAwait(false);

        if (!result.Success || result.Pet is null)
        {
            return result;
        }

        _roomGrain._state.PetsById[petId] = result.Pet;

        if (_motionByPetId.TryGetValue(petId, out PetMotionState? feedMotion))
        {
            feedMotion.IsStatsDirty = false;
        }

        await SendPetUpdatedAsync(result.Pet, ct).ConfigureAwait(false);
        await UpdateFoodItemInLiveStateAsync(
                ctx,
                foodItemId,
                result.UsesRemaining,
                result.FoodState,
                ct
            )
            .ConfigureAwait(false);

        return result;
    }

    public async Task<PetSnapshot?> GetPlacedPetSnapshotAsync(int petId, CancellationToken ct)
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        return _roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? snapshot)
            ? snapshot
            : null;
    }

    public async Task<ImmutableArray<PetSnapshot>> GetPlacedPetSnapshotsAsync(CancellationToken ct)
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        return _roomGrain._state.PetsById.Values.OrderBy(p => p.PetId).ToImmutableArray();
    }

    public async Task<ImmutableArray<RoomAvatarSnapshot>> GetPlacedPetAvatarSnapshotsAsync(
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        List<RoomAvatarSnapshot> snapshots = new(_roomGrain._state.PetsById.Count);

        foreach (PetSnapshot pet in _roomGrain._state.PetsById.Values.OrderBy(p => p.PetId))
        {
            snapshots.Add(await ToAvatarSnapshotAsync(pet, ct).ConfigureAwait(false));
        }

        return snapshots.ToImmutableArray();
    }

    private async Task<RoomPetAvatarSnapshot?> ProcessPetMotionAsync(
        PetSnapshot pet,
        PetMotionState motion,
        long now,
        CancellationToken ct
    )
    {
        if (motion.PendingStopAtMs > 0 && motion.TilePath.Count == 0)
        {
            if (now < motion.PendingStopAtMs)
            {
                return null;
            }

            motion.PendingStopAtMs = 0;
            motion.NextWanderAtMs = ScheduleNextWanderAt(now);

            if (motion.FeedTargetId is RoomObjectId feedId)
            {
                motion.FeedTargetId = null;
                string eatPosture = await AutoFeedPetAtBowlAsync(pet, feedId, ct)
                    .ConfigureAwait(false);
                PetSnapshot petAfterFeed = _roomGrain._state.PetsById.TryGetValue(
                    pet.PetId,
                    out PetSnapshot? fed
                )
                    ? fed
                    : pet;

                if (!string.IsNullOrEmpty(eatPosture))
                {
                    return await ToAvatarSnapshotAsync(petAfterFeed, $"/{eatPosture}/", ct)
                        .ConfigureAwait(false);
                }

                return await ToAvatarSnapshotAsync(petAfterFeed, ct).ConfigureAwait(false);
            }

            return await ToAvatarSnapshotAsync(pet, ct).ConfigureAwait(false);
        }

        if (motion.TilePath.Count == 0 && now >= motion.NextWanderAtMs)
        {
            if (!TryDirectPetToFood(pet, motion, now))
            {
                TryStartWander(pet, motion, now);
            }
        }

        if (motion.TilePath.Count == 0)
        {
            return null;
        }

        int nextTileId = motion.TilePath[0];
        motion.TilePath.RemoveAt(0);

        if (motion.TilePath.Count == 0)
        {
            motion.PendingStopAtMs = _roomGrain.AlignToNextBoundary(
                now,
                _roomGrain._roomConfig.Pet.TickMs
            );
        }

        if (
            !TryPreparePetStep(
                pet,
                motion,
                nextTileId,
                out PetSnapshot facingPet,
                out string status
            )
        )
        {
            motion.ClearMovement();
            motion.NextWanderAtMs = ScheduleNextWanderAt(now);

            return await ToAvatarSnapshotAsync(pet, ct).ConfigureAwait(false);
        }

        _roomGrain._state.PetsById[pet.PetId] = facingPet;

        return await ToAvatarSnapshotAsync(facingPet, status, ct).ConfigureAwait(false);
    }

    private PetSnapshot ApplyPendingPetStep(PetSnapshot pet, PetMotionState motion)
    {
        if (motion.NextTileId < 0)
        {
            return pet;
        }

        int nextTileId = motion.NextTileId;
        motion.NextTileId = -1;

        if (!_roomGrain.MapModule.InBounds(nextTileId))
        {
            return pet;
        }

        (int nextX, int nextY) = _roomGrain.MapModule.GetTileXY(nextTileId);

        if (pet.X == nextX && pet.Y == nextY)
        {
            return pet;
        }

        Altitude nextHeight = _roomGrain._state.TileHeights[nextTileId];
        Rotation direction = RotationExtensions.FromPoints(pet.X, pet.Y, nextX, nextY);
        PetSnapshot updated = pet with
        {
            X = nextX,
            Y = nextY,
            Z = nextHeight.Value,
            Direction = direction,
        };

        _roomGrain._state.PetsById[pet.PetId] = updated;

        return updated;
    }

    private bool TryPreparePetStep(
        PetSnapshot pet,
        PetMotionState motion,
        int nextTileId,
        out PetSnapshot facingPet,
        out string status
    )
    {
        facingPet = pet;
        status = string.Empty;

        int currentTileId = _roomGrain.MapModule.ToIdx(pet.X, pet.Y);

        if (!CanPetWalkBetween(pet.PetId, currentTileId, nextTileId, motion.TilePath.Count == 0))
        {
            return false;
        }

        (int nextX, int nextY) = _roomGrain.MapModule.GetTileXY(nextTileId);

        if (pet.X == nextX && pet.Y == nextY)
        {
            return false;
        }

        Altitude nextHeight = _roomGrain._state.TileHeights[nextTileId];
        Rotation direction = RotationExtensions.FromPoints(pet.X, pet.Y, nextX, nextY);

        facingPet = pet with { Direction = direction };
        motion.NextTileId = nextTileId;
        status = $"/{AvatarStatusType.Move.ToLegacyString()} {nextX},{nextY},{nextHeight}/";

        return true;
    }

    private bool TryStartWander(PetSnapshot pet, PetMotionState motion, long now)
    {
        if (pet.Type == MonsterplantPetType)
        {
            return false;
        }

        motion.NextWanderAtMs = ScheduleNextWanderAt(now);

        if (!_roomGrain.MapModule.InBounds(pet.X, pet.Y))
        {
            return false;
        }

        int radius = Math.Max(1, _roomGrain._roomConfig.Pet.WanderRadius);
        int attempts = Math.Max(1, _roomGrain._roomConfig.Pet.WanderCandidateAttempts);

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            int targetX = pet.X + Random.Shared.Next(-radius, radius + 1);
            int targetY = pet.Y + Random.Shared.Next(-radius, radius + 1);

            if (
                (targetX == pet.X && targetY == pet.Y)
                || !_roomGrain.MapModule.InBounds(targetX, targetY)
            )
            {
                continue;
            }

            int targetTileId = _roomGrain.MapModule.ToIdx(targetX, targetY);

            if (!CanPetOccupyTile(pet.PetId, targetTileId))
            {
                continue;
            }

            IReadOnlyList<(int X, int Y)> path = _roomGrain.PathingSystem.FindPath(
                (pet.X, pet.Y),
                (targetX, targetY),
                tileId => CanPetOccupyTile(pet.PetId, tileId),
                (currentTileId, nextTileId, isGoal) =>
                    CanPetWalkBetween(pet.PetId, currentTileId, nextTileId, isGoal)
            );

            if (path.Count < 2)
            {
                continue;
            }

            motion.TilePath.Clear();
            motion.TilePath.AddRange(
                path.Skip(1).Select(pos => _roomGrain.MapModule.ToIdx(pos.X, pos.Y))
            );

            return true;
        }

        return false;
    }

    private bool CanPetWalkBetween(int petId, int currentTileId, int nextTileId, bool isGoal)
    {
        if (!CanPetOccupyTile(petId, nextTileId))
        {
            return false;
        }

        Altitude currentHeight = _roomGrain._state.TileHeights[currentTileId];
        Altitude nextHeight = _roomGrain._state.TileHeights[nextTileId];

        if (Math.Abs(nextHeight - currentHeight) > Math.Abs(_roomGrain._roomConfig.MaxStepHeight))
        {
            return false;
        }

        if (
            !_roomGrain._roomConfig.EnableDiagonalChecking
            || !_roomGrain.MapModule.IsDiagonal(currentTileId, nextTileId)
        )
        {
            return true;
        }

        (int fromX, int fromY) = _roomGrain.MapModule.GetTileXY(currentTileId);
        (int toX, int toY) = _roomGrain.MapModule.GetTileXY(nextTileId);
        bool left = CanPetOccupyTile(petId, _roomGrain.MapModule.ToIdx(toX, fromY));
        bool right = CanPetOccupyTile(petId, _roomGrain.MapModule.ToIdx(fromX, toY));

        return left || right;
    }

    private bool CanPetOccupyTile(int petId, int tileIdx)
    {
        if (!_roomGrain.MapModule.InBounds(tileIdx))
        {
            return false;
        }

        RoomTileFlags flags = _roomGrain._state.TileFlags[tileIdx];

        if (
            flags.Has(RoomTileFlags.Disabled)
            || flags.Has(RoomTileFlags.Closed)
            || flags.Has(RoomTileFlags.AvatarOccupied)
        )
        {
            return false;
        }

        if (flags.Has(RoomTileFlags.FurnitureOccupied) && !flags.Has(RoomTileFlags.Walkable))
        {
            return false;
        }

        return !IsPetTileOccupied(petId, tileIdx);
    }

    private bool IsPetTileOccupied(int petId, int tileIdx)
    {
        foreach (PetSnapshot pet in _roomGrain._state.PetsById.Values)
        {
            if (pet.PetId == petId)
            {
                continue;
            }

            if (_roomGrain.MapModule.ToIdx(pet.X, pet.Y) == tileIdx)
            {
                return true;
            }
        }

        return false;
    }

    private PetSnapshot ApplyNeedDecay(PetSnapshot pet, PetMotionState motion, long now)
    {
        long elapsedMs = now - motion.LastStatDecayAtMs;

        if (elapsedMs <= 0)
        {
            return pet;
        }

        double elapsedMinutes = elapsedMs / 60_000.0;

        int nutritionCap = _roomGrain._petLevelProvider.GetNutritionCapForLevel(
            pet.Type,
            pet.Level
        );
        int energyCap = _roomGrain._petLevelProvider.GetEnergyCapForLevel(pet.Type, pet.Level);

        int newNutrition = pet.Nutrition;
        int newEnergy = pet.Energy;

        int nutritionLoss = (int)(
            elapsedMinutes * _roomGrain._roomConfig.Pet.NutritionDecayPerMinute
        );

        if (nutritionLoss > 0)
        {
            newNutrition = Math.Clamp(pet.Nutrition - nutritionLoss, 0, nutritionCap);
        }

        if (motion.IsSleeping)
        {
            double nestMultiplier = IsOnNestTile(pet)
                ? _roomGrain._roomConfig.Pet.NestEnergyMultiplier
                : 1.0;
            int energyGain = (int)(
                elapsedMinutes
                * _roomGrain._roomConfig.Pet.EnergyDecayPerMinute
                * 2
                * nestMultiplier
            );

            if (energyGain > 0)
            {
                newEnergy = Math.Clamp(pet.Energy + energyGain, 0, energyCap);
            }

            if (newEnergy >= _roomGrain._roomConfig.Pet.SleepWakeEnergyThreshold)
            {
                motion.IsSleeping = false;
                motion.SleepPostureSent = false;
                motion.PendingWakeVocal = true;
            }
        }
        else
        {
            int energyLoss = (int)(
                elapsedMinutes * _roomGrain._roomConfig.Pet.EnergyDecayPerMinute
            );

            if (energyLoss > 0)
            {
                newEnergy = Math.Clamp(pet.Energy - energyLoss, 0, energyCap);
            }

            if (newEnergy == 0 && !motion.IsSleeping)
            {
                motion.IsSleeping = true;
                motion.SleepPostureSent = false;
                motion.PendingSleepVocal = true;
                motion.ClearMovement();
            }
        }

        if (nutritionLoss == 0 && newEnergy == pet.Energy)
        {
            return pet;
        }

        motion.LastStatDecayAtMs = now;

        if (newNutrition == pet.Nutrition && newEnergy == pet.Energy)
        {
            return pet;
        }

        motion.IsStatsDirty = true;

        PetSnapshot updated = pet with { Nutrition = newNutrition, Energy = newEnergy };
        _roomGrain._state.PetsById[pet.PetId] = updated;

        return updated;
    }

    private void ApplyOfflineDecay(PetEntity entity, long nowMs)
    {
        long entityUpdatedMs = new DateTimeOffset(
            DateTime.SpecifyKind(entity.UpdatedAt, DateTimeKind.Utc)
        ).ToUnixTimeMilliseconds();

        long elapsedMs = Math.Max(0L, nowMs - entityUpdatedMs);

        if (elapsedMs <= 0)
        {
            return;
        }

        double elapsedMinutes = elapsedMs / 60_000.0;
        entity.Nutrition = Math.Max(
            0,
            entity.Nutrition
                - (int)(elapsedMinutes * _roomGrain._roomConfig.Pet.NutritionDecayPerMinute)
        );
        entity.Energy = Math.Max(
            0,
            entity.Energy - (int)(elapsedMinutes * _roomGrain._roomConfig.Pet.EnergyDecayPerMinute)
        );
    }

    private async Task FlushDirtyPetsAsync(CancellationToken ct)
    {
        List<int> dirtyIds = [];

        foreach (KeyValuePair<int, PetMotionState> kvp in _motionByPetId)
        {
            if (kvp.Value.IsStatsDirty)
            {
                dirtyIds.Add(kvp.Key);
            }
        }

        if (dirtyIds.Count == 0)
        {
            return;
        }

        await using TurboDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PetEntity[] entities = await dbCtx
            .Pets.Where(p => dirtyIds.Contains(p.Id) && p.DeletedAt == null)
            .ToArrayAsync(ct)
            .ConfigureAwait(false);

        foreach (PetEntity entity in entities)
        {
            if (!_roomGrain._state.PetsById.TryGetValue(entity.Id, out PetSnapshot? snapshot))
            {
                continue;
            }

            entity.Nutrition = snapshot.Nutrition;
            entity.Energy = snapshot.Energy;
            entity.Experience = snapshot.Experience;
            entity.Level = snapshot.Level;
            entity.Respect = snapshot.Respect;
            entity.RespectTodayCount = snapshot.RespectTodayCount;
            entity.RespectLastResetDate = snapshot.RespectLastResetDate;
            entity.CanBreed = snapshot.CanBreed;

            if (_motionByPetId.TryGetValue(entity.Id, out PetMotionState? motion))
            {
                motion.IsStatsDirty = false;
            }
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task SyncLiveStatsToPetEntityAsync(
        TurboDbContext dbCtx,
        int petId,
        CancellationToken ct
    )
    {
        if (!_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? live))
        {
            return;
        }

        PetEntity? entity = await dbCtx
            .Pets.SingleOrDefaultAsync(p => p.Id == petId && p.DeletedAt == null, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return;
        }

        entity.Nutrition = live.Nutrition;
        entity.Energy = live.Energy;
        entity.Experience = live.Experience;
        entity.Level = live.Level;
        entity.Respect = live.Respect;
        entity.RespectTodayCount = live.RespectTodayCount;
        entity.RespectLastResetDate = live.RespectLastResetDate;
        entity.CanBreed = live.CanBreed;
    }

    private PetMotionState GetMotionState(PetSnapshot pet, long now)
    {
        if (_motionByPetId.TryGetValue(pet.PetId, out PetMotionState? motion))
        {
            return motion;
        }

        motion = new PetMotionState
        {
            NextWanderAtMs = ScheduleNextWanderAt(now),
            LastStatDecayAtMs = now,
            IsSleeping = pet.Energy <= 0,
        };
        _motionByPetId[pet.PetId] = motion;

        return motion;
    }

    private long ScheduleNextWanderAt(long now)
    {
        int minMs = Math.Max(
            _roomGrain._roomConfig.Pet.TickMs,
            _roomGrain._roomConfig.Pet.WanderIdleMinMs
        );
        int maxMs = Math.Max(minMs, _roomGrain._roomConfig.Pet.WanderIdleMaxMs);

        return now + Random.Shared.Next(minMs, maxMs + 1);
    }

    private long ScheduleNextVocalAt(long now)
    {
        int intervalMs = _roomGrain._roomConfig.Pet.VocalIntervalMs;
        int minMs = intervalMs * 3 / 4;
        int maxMs = intervalMs * 5 / 4;

        return now + Random.Shared.Next(minMs, maxMs + 1);
    }

    private double GetTileHeightForPet(int x, int y)
    {
        if (!_roomGrain.MapModule.InBounds(x, y))
        {
            throw new TurboException(TurboErrorCodeEnum.TileOutOfBounds);
        }

        int tileIdx = _roomGrain.MapModule.ToIdx(x, y);
        RoomTileFlags flags = _roomGrain._state.TileFlags[tileIdx];

        if (
            flags.Has(RoomTileFlags.Disabled)
            || flags.Has(RoomTileFlags.Closed)
            || flags.Has(RoomTileFlags.AvatarOccupied)
        )
        {
            throw new TurboException(TurboErrorCodeEnum.InvalidMoveTarget);
        }

        return _roomGrain._state.TileHeights[tileIdx].Value;
    }

    private async Task EnsureRoomReadyForPetPlacementAsync(CancellationToken ct)
    {
        await _roomGrain.MapModule.EnsureMapBuiltAsync(ct).ConfigureAwait(false);
        await _roomGrain.FurniModule.EnsureFurniLoadedAsync(ct).ConfigureAwait(false);
    }

    public async Task<PetSnapshot?> RespectPetAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        if (!_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? pet))
        {
            return null;
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (pet.RespectLastResetDate != today)
        {
            pet = pet with { RespectTodayCount = 0, RespectLastResetDate = today };
            _roomGrain._state.PetsById[petId] = pet;
        }

        int dailyCap = _roomGrain._roomConfig.Pet.RespectDailyCapPerPet;

        if (pet.RespectTodayCount >= dailyCap)
        {
            return pet;
        }

        int respectXp = _roomGrain._roomConfig.Pet.RespectXpReward;
        PetSnapshot updated = await GrantXpAndLevelUpAsync(pet, respectXp, ct)
            .ConfigureAwait(false);
        updated = updated with
        {
            Respect = updated.Respect + 1,
            RespectTodayCount = updated.RespectTodayCount + 1,
            RespectLastResetDate = today,
        };
        _roomGrain._state.PetsById[petId] = updated;

        if (_motionByPetId.TryGetValue(petId, out PetMotionState? motion))
        {
            motion.IsStatsDirty = true;
        }

        await SendPetUpdatedAsync(updated, ct).ConfigureAwait(false);
        await BroadcastPetRespectNotificationAsync(updated).ConfigureAwait(false);

        return updated;
    }

    public async Task<PetSnapshot?> GrantPetCommandXpAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        if (!_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? pet))
        {
            return null;
        }

        int commandXp = _roomGrain._roomConfig.Pet.CommandXpReward;
        PetSnapshot updated = await GrantXpAndLevelUpAsync(pet, commandXp, ct)
            .ConfigureAwait(false);
        _roomGrain._state.PetsById[petId] = updated;

        if (_motionByPetId.TryGetValue(petId, out PetMotionState? motion))
        {
            motion.IsStatsDirty = true;
        }

        await SendPetUpdatedAsync(updated, ct).ConfigureAwait(false);

        return updated;
    }

    public async Task<PetSnapshot?> GiveSupplementToPetAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        if (!_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? pet))
        {
            return null;
        }

        int energyCap = _roomGrain._petLevelProvider.GetEnergyCapForLevel(pet.Type, pet.Level);
        int newEnergy = Math.Min(
            pet.Energy + _roomGrain._roomConfig.Pet.SupplementEnergyBoost,
            energyCap
        );

        int supplementXp = _roomGrain._roomConfig.Pet.SupplementXpReward;
        PetSnapshot withEnergy = pet with { Energy = newEnergy };
        _roomGrain._state.PetsById[petId] = withEnergy;

        PetSnapshot updated = await GrantXpAndLevelUpAsync(withEnergy, supplementXp, ct)
            .ConfigureAwait(false);
        _roomGrain._state.PetsById[petId] = updated;

        bool petWokeUp = false;

        if (_motionByPetId.TryGetValue(petId, out PetMotionState? motion))
        {
            motion.IsStatsDirty = true;
            if (
                motion.IsSleeping
                && updated.Energy >= _roomGrain._roomConfig.Pet.SleepWakeEnergyThreshold
            )
            {
                motion.IsSleeping = false;
                motion.SleepPostureSent = false;
                petWokeUp = true;
            }
        }

        await SendPetUpdatedAsync(updated, ct).ConfigureAwait(false);

        if (petWokeUp)
        {
            await BroadcastPetVocalAsync(updated, "GENERIC_HAPPY").ConfigureAwait(false);
        }

        return updated;
    }

    private Task BroadcastPetVocalAsync(PetSnapshot pet, string vocalType)
    {
        return _roomGrain.SendComposerToRoomAsync(
            new PetVocalMessageComposer
            {
                PetObjectId = RoomPetRuntime.ToRoomObjectId(pet.PetId),
                PetType = pet.Type,
                VocalType = vocalType,
                VocalIndex = GetVocalIndex(pet.Type, vocalType),
            }
        );
    }

    private Task BroadcastPetRespectNotificationAsync(PetSnapshot pet)
    {
        return _roomGrain.SendComposerToRoomAsync(
            new PetRespectNotificationEventMessageComposer
            {
                PetRespect = pet.Respect,
                PetOwnerId = pet.OwnerId.Value,
                PetId = pet.PetId,
                PetName = pet.Name,
                PetType = pet.Type,
                PetColor = pet.Color,
                PetRace = pet.Race,
                PetLevel = pet.Level,
            }
        );
    }

    private bool IsOnNestTile(PetSnapshot pet)
    {
        foreach (IRoomItem item in _roomGrain._state.ItemsById.Values)
        {
            if (
                item.X == pet.X
                && item.Y == pet.Y
                && item.Definition.LogicName == _roomGrain._roomConfig.Pet.NestLogicName
            )
            {
                return true;
            }
        }

        return false;
    }

    private string SelectVocalForState(PetSnapshot pet, PetMotionState motion)
    {
        if (motion.IsSleeping)
        {
            return "SLEEPING";
        }

        bool isHungry = pet.Nutrition < _roomGrain._roomConfig.Pet.HungerThreshold;
        bool isThirsty =
            pet.Energy < _roomGrain._roomConfig.Pet.ThirstThreshold
            && pet.Energy > _roomGrain._roomConfig.Pet.SleepWakeEnergyThreshold;
        bool isTired =
            pet.Energy > 0 && pet.Energy <= _roomGrain._roomConfig.Pet.SleepWakeEnergyThreshold;

        if (isHungry)
        {
            return "HUNGRY";
        }

        if (isThirsty)
        {
            return "THIRSTY";
        }

        if (isTired)
        {
            return "TIRED";
        }

        return Random.Shared.Next(0, 3) switch
        {
            0 => "GENERIC_NEUTRAL",
            1 => "GENERIC_HAPPY",
            _ => "PLAYFUL",
        };
    }

    private static int GetVocalIndex(int petType, string vocalType)
    {
        // Max is the exclusive upper bound; Random.Next(0, max) → indices 0..max-1
        int max = (petType, vocalType) switch
        {
            (35, "nlDISOBEY") => 3,
            (35, "DRINKING") => 2,
            (35, "EATING") => 3,
            (35, "GENERIC_HAPPY") => 3,
            (35, "GENERIC_NEUTRAL") => 3,
            (35, "GENERIC_SAD") => 2,
            (35, "GREET_OWNER") => 3,
            (35, "HUNGRY") => 4,
            (35, "LEVEL_UP") => 4,
            (35, "MUTED") => 1,
            (35, "PLAYFUL") => 2,
            (35, "PLAYING") => 2,
            (35, "SLEEPING") => 3,
            (35, "THIRSTY") => 3,
            (35, "TIRED") => 3,
            (35, "UNKNOWN_COMMAND") => 2,
            (15, "DISOBEY") => 3,
            (15, "DRINKING") => 2,
            (15, "EATING") => 3,
            (15, "GENERIC_HAPPY") => 3,
            (15, "GENERIC_NEUTRAL") => 3,
            (15, "GENERIC_SAD") => 2,
            (15, "GREET_OWNER") => 3,
            (15, "HUNGRY") => 4,
            (15, "LEVEL_UP") => 4,
            (15, "MUTED") => 1,
            (15, "PLAYFUL") => 2,
            (15, "PLAYING") => 2,
            (15, "SLEEPING") => 3,
            (15, "THIRSTY") => 3,
            (15, "TIRED") => 3,
            (15, "UNKNOWN_COMMAND") => 2,
            _ => 3,
        };
        return max > 1 ? Random.Shared.Next(0, max) : 0;
    }

    public async Task<PetSnapshot?> IssueCommandAsync(
        ActionContext ctx,
        int petId,
        int commandId,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        if (!_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? pet))
        {
            return null;
        }

        if (pet.OwnerId != ctx.PlayerId)
        {
            return null;
        }

        PetCommandEntry? cmd = _roomGrain._petCommandProvider.GetCommandConfig(pet.Type, commandId);

        if (cmd is null)
        {
            return null;
        }

        if (pet.Level < cmd.LevelRequired)
        {
            await BroadcastPetVocalAsync(pet, "UNKNOWN_COMMAND").ConfigureAwait(false);
            return null;
        }

        if (_motionByPetId.TryGetValue(petId, out PetMotionState? motion) && motion.IsSleeping)
        {
            await BroadcastPetVocalAsync(pet, "SLEEPING").ConfigureAwait(false);
            return null;
        }

        if (pet.Energy < cmd.EnergyCost)
        {
            await BroadcastPetVocalAsync(pet, "TIRED").ConfigureAwait(false);
            return null;
        }

        int newEnergy = pet.Energy - cmd.EnergyCost;
        PetSnapshot withEnergy = pet with { Energy = newEnergy };
        _roomGrain._state.PetsById[petId] = withEnergy;

        PetSnapshot updated = await GrantXpAndLevelUpAsync(withEnergy, cmd.XpReward, ct)
            .ConfigureAwait(false);
        _roomGrain._state.PetsById[petId] = updated;

        if (motion is not null)
        {
            motion.IsStatsDirty = true;

            if (newEnergy == 0)
            {
                motion.IsSleeping = true;
                motion.SleepPostureSent = false;
            }
        }

        if (!string.IsNullOrEmpty(cmd.Posture))
        {
            RoomPetAvatarSnapshot postureSnapshot = await ToAvatarSnapshotAsync(
                    updated,
                    $"/{cmd.Posture}/",
                    ct
                )
                .ConfigureAwait(false);

            await _roomGrain
                .SendComposerToRoomAsync(
                    new UserUpdateMessageComposer { Avatars = [postureSnapshot] }
                )
                .ConfigureAwait(false);
        }
        else
        {
            await SendPetUpdatedAsync(updated, ct).ConfigureAwait(false);
        }

        return updated;
    }

    public async Task TogglePetBreedingPermissionAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        if (!_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? pet))
        {
            return;
        }

        if (pet.OwnerId != ctx.PlayerId)
        {
            return;
        }

        PetSnapshot updated = pet with { CanBreed = !pet.CanBreed };
        _roomGrain._state.PetsById[petId] = updated;

        if (_motionByPetId.TryGetValue(petId, out PetMotionState? motion))
        {
            motion.IsStatsDirty = true;
        }
    }

    public async Task<bool> BreedPetsAsync(
        ActionContext ctx,
        int petOneId,
        int petTwoId,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        if (
            !_roomGrain._state.PetsById.TryGetValue(petOneId, out PetSnapshot? petOne)
            || !_roomGrain._state.PetsById.TryGetValue(petTwoId, out PetSnapshot? petTwo)
        )
        {
            await SendBreedingFailureAsync(ctx.PlayerId, 3, ct).ConfigureAwait(false);
            return false;
        }

        if (petOne.Type != petTwo.Type)
        {
            await SendBreedingFailureAsync(ctx.PlayerId, 1, ct).ConfigureAwait(false);
            return false;
        }

        if (!petTwo.CanBreed)
        {
            await SendBreedingFailureAsync(ctx.PlayerId, 2, ct).ConfigureAwait(false);
            return false;
        }

        int proposedRace = Random.Shared.Next(0, 2) == 0 ? petOne.Race : petTwo.Race;
        string proposedColor = BlendColors(petOne.Color, petTwo.Color);
        int proposedGender = Random.Shared.Next(0, 2);

        PendingBreedingSession session = new(
            petOneId,
            petTwoId,
            petOne.OwnerId,
            petTwo.OwnerId,
            proposedRace,
            proposedColor,
            proposedGender
        );

        _breedingByPetOneId[petOneId] = session;

        PetBreedingEventMessageComposer requesterMsg = new()
        {
            PetOneId = petOneId,
            PetTwoId = petTwoId,
            OwnerOneId = petOne.OwnerId.Value,
            OwnerTwoId = petTwo.OwnerId.Value,
            ProposedRace = proposedRace,
            ProposedColor = proposedColor,
            ProposedGender = proposedGender,
        };

        ConfirmBreedingRequestEventMessageComposer targetMsg = new()
        {
            PetOneId = petOneId,
            PetTwoId = petTwoId,
            OwnerOneId = petOne.OwnerId.Value,
            OwnerTwoId = petTwo.OwnerId.Value,
            ProposedRace = proposedRace,
            ProposedColor = proposedColor,
            ProposedGender = proposedGender,
        };

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(petOne.OwnerId)
            .SendComposerAsync(requesterMsg)
            .ConfigureAwait(false);

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(petTwo.OwnerId)
            .SendComposerAsync(targetMsg)
            .ConfigureAwait(false);

        return true;
    }

    public async Task<bool> ConfirmPetBreedingAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        PendingBreedingSession? session = _breedingByPetOneId.Values.FirstOrDefault(s =>
            s.PetTwoId == petId
        );

        if (session is null)
        {
            return false;
        }

        _breedingByPetOneId.Remove(session.PetOneId);

        await using TurboDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PetEntity baby = new()
        {
            OwnerPlayerEntityId = session.OwnerOneId.Value,
            RoomEntityId = null,
            Name = "Baby",
            Type = _roomGrain._state.PetsById.TryGetValue(session.PetOneId, out PetSnapshot? p1)
                ? p1.Type
                : 0,
            Race = session.ProposedRace,
            Color = session.ProposedColor,
            Gender = session.ProposedGender == 0 ? AvatarGenderType.Male : AvatarGenderType.Female,
            Level = 1,
            Experience = 0,
            Energy = _roomGrain._roomConfig.Pet.EnergyCap,
            Nutrition = _roomGrain._roomConfig.Pet.NutritionCap,
            Respect = 0,
            X = 0,
            Y = 0,
            Z = 0,
            Direction = (int)Rotation.South,
            ParentOneId = session.PetOneId,
            ParentTwoId = session.PetTwoId,
            OwnerPlayerEntity = null!,
        };

        dbCtx.Pets.Add(baby);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        ConfirmBreedingResultEventMessageComposer resultMsg = new()
        {
            Success = true,
            NewPetId = baby.Id,
        };

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(session.OwnerOneId)
            .SendComposerAsync(resultMsg)
            .ConfigureAwait(false);

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(session.OwnerTwoId)
            .SendComposerAsync(resultMsg)
            .ConfigureAwait(false);

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(session.OwnerOneId)
            .SendComposerAsync(new NestBreedingSuccessEventMessageComposer { NewPetId = baby.Id })
            .ConfigureAwait(false);

        await _roomGrain
            .SendComposerToRoomAsync(
                new PetBreedingResultEventMessageComposer
                {
                    PetOneId = session.PetOneId,
                    PetTwoId = session.PetTwoId,
                    Result = 0,
                }
            )
            .ConfigureAwait(false);

        return true;
    }

    public Task CancelPetBreedingAsync(ActionContext ctx, int petId, CancellationToken ct)
    {
        int keyToRemove = -1;

        foreach (KeyValuePair<int, PendingBreedingSession> kvp in _breedingByPetOneId)
        {
            if (kvp.Key == petId || kvp.Value.PetTwoId == petId)
            {
                keyToRemove = kvp.Key;
                break;
            }
        }

        if (keyToRemove >= 0)
        {
            _breedingByPetOneId.Remove(keyToRemove);
        }

        return Task.CompletedTask;
    }

    private static string BlendColors(string colorA, string colorB)
    {
        if (colorA.Length != 6 || colorB.Length != 6)
        {
            return colorA;
        }

        char[] result = new char[6];

        for (int i = 0; i < 6; i++)
        {
            result[i] = i % 2 == 0 ? colorA[i] : colorB[i];
        }

        return new string(result);
    }

    private async Task SendBreedingFailureAsync(PlayerId playerId, int reason, CancellationToken ct)
    {
        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(playerId)
            .SendComposerAsync(new GoToBreedingNestFailureEventMessageComposer { Reason = reason })
            .ConfigureAwait(false);
    }

    private async Task<PetSnapshot> GrantXpAndLevelUpAsync(
        PetSnapshot pet,
        int xp,
        CancellationToken ct
    )
    {
        int newExperience = pet.Experience + xp;
        int newLevel = _roomGrain._petLevelProvider.GetLevelForExperience(pet.Type, newExperience);
        bool leveledUp = newLevel > pet.Level;
        int maxLevel = _roomGrain._petLevelProvider.GetMaxLevel(pet.Type);

        PetSnapshot updated = pet with { Experience = newExperience, Level = newLevel };
        _roomGrain._state.PetsById[pet.PetId] = updated;

        int xpForNext = _roomGrain._petLevelProvider.GetExperienceForNextLevel(
            updated.Type,
            updated.Level
        );
        int xpForNextSafe = xpForNext == int.MaxValue ? updated.Experience : xpForNext;

        await _roomGrain
            .SendComposerToRoomAsync(
                new PetExperienceMessageComposer
                {
                    PetId = updated.PetId,
                    Experience = updated.Experience,
                    ExperienceForNextLevel = xpForNextSafe,
                    Level = updated.Level,
                    MaxLevel = maxLevel,
                }
            )
            .ConfigureAwait(false);

        if (leveledUp)
        {
            await _roomGrain
                .SendComposerToRoomAsync(
                    new PetLevelUpdateMessageComposer
                    {
                        PetId = updated.PetId,
                        Level = updated.Level,
                    }
                )
                .ConfigureAwait(false);

            await _roomGrain
                .SendComposerToRoomAsync(
                    new UserUpdateMessageComposer
                    {
                        Avatars = [await ToAvatarSnapshotAsync(updated, ct).ConfigureAwait(false)],
                    }
                )
                .ConfigureAwait(false);

            await BroadcastPetVocalAsync(updated, "LEVEL_UP").ConfigureAwait(false);

            try
            {
                await _roomGrain
                    ._grainFactory.GetPlayerPresenceGrain(updated.OwnerId)
                    .SendComposerAsync(
                        new PetLevelNotificationEventMessageComposer
                        {
                            PetId = updated.PetId,
                            NewLevel = updated.Level,
                            PetName = updated.Name,
                        }
                    )
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogError(
                    ex,
                    "Failed to send pet level-up notification for pet {PetId}",
                    updated.PetId
                );
            }
        }

        return updated;
    }

    private async Task SendPetAddedAsync(PetSnapshot pet, CancellationToken ct)
    {
        RoomPetAvatarSnapshot snapshot = await ToAvatarSnapshotAsync(pet, ct).ConfigureAwait(false);

        await _roomGrain
            .SendComposerToRoomAsync(new UsersMessageComposer { Avatars = [snapshot] })
            .ConfigureAwait(false);
    }

    private async Task SendPetUpdatedAsync(PetSnapshot pet, CancellationToken ct)
    {
        RoomPetAvatarSnapshot snapshot = await ToAvatarSnapshotAsync(pet, ct).ConfigureAwait(false);

        await _roomGrain
            .SendComposerToRoomAsync(new UserUpdateMessageComposer { Avatars = [snapshot] })
            .ConfigureAwait(false);
    }

    private async Task SendPetRemovedFromInventoryAsync(PetSnapshot pet)
    {
        try
        {
            await _roomGrain
                ._grainFactory.GetPlayerPresenceGrain(pet.OwnerId)
                .SendComposerAsync(
                    new PetRemovedFromInventoryEventMessageComposer { PetId = pet.PetId }
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogError(
                ex,
                "Failed to send pet {PetId} inventory removal for player {PlayerId}",
                pet.PetId,
                pet.OwnerId
            );
        }
    }

    private async Task SendPetPlacingErrorAsync(ActionContext ctx, int errorCode)
    {
        try
        {
            await _roomGrain
                ._grainFactory.GetPlayerPresenceGrain(ctx.PlayerId)
                .SendComposerAsync(new PetPlacingErrorMessageComposer { ErrorCode = errorCode })
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogError(
                ex,
                "Failed to send pet placing error {ErrorCode} to player {PlayerId}",
                errorCode,
                ctx.PlayerId
            );
        }
    }

    private async Task SendPetAddedToInventoryAsync(PetSnapshot pet, CancellationToken ct)
    {
        try
        {
            await _roomGrain
                ._grainFactory.GetPlayerPresenceGrain(pet.OwnerId)
                .OnPetAddedToInventoryAsync(pet, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogError(
                ex,
                "Failed to send pet {PetId} inventory add for player {PlayerId}",
                pet.PetId,
                pet.OwnerId
            );
        }
    }

    private async Task<RoomPetAvatarSnapshot> ToAvatarSnapshotAsync(
        PetSnapshot pet,
        CancellationToken ct
    )
    {
        return await ToAvatarSnapshotAsync(pet, string.Empty, string.Empty, ct)
            .ConfigureAwait(false);
    }

    private async Task<RoomPetAvatarSnapshot> ToAvatarSnapshotAsync(
        PetSnapshot pet,
        string status,
        CancellationToken ct
    )
    {
        return await ToAvatarSnapshotAsync(pet, status, string.Empty, ct).ConfigureAwait(false);
    }

    private async Task<RoomPetAvatarSnapshot> ToAvatarSnapshotAsync(
        PetSnapshot pet,
        string status,
        string posture,
        CancellationToken ct
    )
    {
        string ownerName = await GetOwnerNameAsync(pet.OwnerId, ct).ConfigureAwait(false);

        return RoomPetRuntime.ToAvatarSnapshot(pet, ownerName, status, posture);
    }

    private async Task<string> GetOwnerNameAsync(PlayerId ownerId, CancellationToken ct)
    {
        if (_roomGrain._state.OwnerNamesById.TryGetValue(ownerId, out string? ownerName))
        {
            return ownerName;
        }

        ownerName = await _roomGrain
            ._grainFactory.GetPlayerDirectoryGrain()
            .GetPlayerNameAsync(ownerId, ct)
            .ConfigureAwait(false);

        _roomGrain._state.OwnerNamesById[ownerId] = ownerName;

        return ownerName;
    }

    private async Task<PetEntity?> LoadPlacedPetAsync(
        TurboDbContext dbCtx,
        int petId,
        CancellationToken ct
    )
    {
        return await dbCtx
            .Pets.SingleOrDefaultAsync(
                p =>
                    p.Id == petId
                    && p.RoomEntityId == _roomGrain.RoomId.Value
                    && p.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);
    }

    private static void EnsurePetOwner(ActionContext ctx, PetEntity pet)
    {
        if (pet.OwnerPlayerEntityId != ctx.PlayerId)
        {
            throw new TurboException(TurboErrorCodeEnum.NoPermissionToManipulatePet);
        }
    }

    private bool TryDirectPetToFood(PetSnapshot pet, PetMotionState motion, long now)
    {
        if (pet.Type == MonsterplantPetType || !_roomGrain._state.RoomSnapshot.AllowPetsEat)
        {
            return false;
        }

        bool needsFood = pet.Nutrition < _roomGrain._roomConfig.Pet.HungerThreshold;
        bool needsDrink = pet.Energy < _roomGrain._roomConfig.Pet.ThirstThreshold;

        if (!needsFood && !needsDrink)
        {
            return false;
        }

        IRoomItem? target = null;
        int bestDist = int.MaxValue;

        foreach (IRoomItem item in _roomGrain._state.ItemsById.Values)
        {
            string logicName = item.Definition.LogicName;
            bool isFood = needsFood && logicName == _roomGrain._roomConfig.Pet.FoodLogicName;
            bool isDrink = needsDrink && logicName == _roomGrain._roomConfig.Pet.DrinkLogicName;

            if (!isFood && !isDrink)
            {
                continue;
            }

            if (item.Logic.GetState() <= 0)
            {
                continue;
            }

            int dist = Math.Abs(pet.X - item.X) + Math.Abs(pet.Y - item.Y);

            if (dist < bestDist)
            {
                bestDist = dist;
                target = item;
            }
        }

        if (target is null)
        {
            return false;
        }

        IReadOnlyList<(int X, int Y)> path = _roomGrain.PathingSystem.FindPath(
            (pet.X, pet.Y),
            (target.X, target.Y),
            tileId => CanPetOccupyTile(pet.PetId, tileId),
            (currentTileId, nextTileId, isGoal) =>
                CanPetWalkBetween(pet.PetId, currentTileId, nextTileId, isGoal)
        );

        if (path.Count < 2)
        {
            return false;
        }

        motion.TilePath.Clear();
        motion.TilePath.AddRange(
            path.Skip(1).Select(pos => _roomGrain.MapModule.ToIdx(pos.X, pos.Y))
        );
        motion.FeedTargetId = target.ObjectId;
        motion.NextWanderAtMs = ScheduleNextWanderAt(now);

        return true;
    }

    private async Task<string> AutoFeedPetAtBowlAsync(
        PetSnapshot pet,
        RoomObjectId feedItemId,
        CancellationToken ct
    )
    {
        if (!_roomGrain._state.ItemsById.TryGetValue(feedItemId, out IRoomItem? item))
        {
            return string.Empty;
        }

        if (item.X != pet.X || item.Y != pet.Y)
        {
            return string.Empty;
        }

        bool isDrink = item.Definition.LogicName == _roomGrain._roomConfig.Pet.DrinkLogicName;

        ActionContext ctx = ActionContext.CreateForPlayer(pet.OwnerId, _roomGrain.RoomId);
        PetFeedResult result = await FeedPetAsync(ctx, pet.PetId, feedItemId, ct)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return string.Empty;
        }

        if (_roomGrain._state.PetsById.TryGetValue(pet.PetId, out PetSnapshot? updated))
        {
            string eatVocal = isDrink ? "DRINKING" : "EATING";
            await BroadcastPetVocalAsync(updated, eatVocal).ConfigureAwait(false);
        }

        return isDrink ? "drk" : "eat";
    }

    private async Task UpdateFoodItemInLiveStateAsync(
        ActionContext ctx,
        RoomObjectId foodItemId,
        int usesRemaining,
        int foodState,
        CancellationToken ct
    )
    {
        if (!_roomGrain._state.ItemsById.TryGetValue(foodItemId, out IRoomItem? item))
        {
            return;
        }

        if (usesRemaining > 0)
        {
            item.Logic.StuffData.SetState(foodState.ToString());
            item.SetExtraData(foodState.ToString());
            await _roomGrain
                .SendComposerToRoomAsync(item.GetRefreshStuffDataComposer())
                .ConfigureAwait(false);
            return;
        }

        if (!_roomGrain.MapModule.RemoveItem(item))
        {
            return;
        }

        await _roomGrain
            .SendComposerToRoomAsync(item.GetRemoveComposer(ctx.PlayerId, true))
            .ConfigureAwait(false);

        await item.Logic.OnDetachAsync(ct).ConfigureAwait(false);
        item.SetAction(null);
        _roomGrain._state.ItemsById.Remove(foodItemId);
    }

    public async Task<PetSnapshot?> PlantMonsterplantSeedAsync(
        ActionContext ctx,
        RoomObjectId seedItemId,
        CancellationToken ct
    )
    {
        await EnsureRoomReadyForPetPlacementAsync(ct).ConfigureAwait(false);

        if (!_roomGrain._state.ItemsById.TryGetValue(seedItemId, out IRoomItem? seedItem))
        {
            return null;
        }

        int x = seedItem.X;
        int y = seedItem.Y;
        double z = seedItem.Z;
        PlayerId ownerId = seedItem.OwnerId;

        await using TurboDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PetEntity plant = new()
        {
            OwnerPlayerEntityId = ownerId.Value,
            OwnerPlayerEntity = null!,
            RoomEntityId = _roomGrain.RoomId.Value,
            Name = "Monsterplant",
            Type = MonsterplantPetType,
            Race = 0,
            Color = "ffffff",
            Gender = AvatarGenderType.Male,
            Level = 1,
            Experience = 0,
            Energy = _roomGrain._roomConfig.Pet.EnergyCap,
            Nutrition = _roomGrain._roomConfig.Pet.NutritionCap,
            Respect = 0,
            RarityLevel = Random.Shared.Next(1, 8),
            LastWateredAt = DateTime.UtcNow,
            X = x,
            Y = y,
            Z = z,
            Direction = (int)Rotation.South,
        };

        dbCtx.Pets.Add(plant);

        FurnitureEntity seedEntity = new() { Id = seedItemId.Value };
        dbCtx.Attach(seedEntity);
        seedEntity.DeletedAt = DateTime.UtcNow;
        dbCtx.Entry(seedEntity).Property(f => f.DeletedAt).IsModified = true;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        PetSnapshot snapshot = RoomPetRuntime.ToSnapshot(plant);
        _roomGrain._state.PetsById[plant.Id] = snapshot;

        await SendPetAddedAsync(snapshot, ct).ConfigureAwait(false);

        await _roomGrain
            .ObjectModule.RemoveObjectAsync(ctx, seedItem, ct, ownerId)
            .ConfigureAwait(false);

        return snapshot;
    }

    private sealed record PendingBreedingSession(
        int PetOneId,
        int PetTwoId,
        PlayerId OwnerOneId,
        PlayerId OwnerTwoId,
        int ProposedRace,
        string ProposedColor,
        int ProposedGender
    );

    private sealed class PetMotionState
    {
        public List<int> TilePath { get; } = [];
        public int NextTileId { get; set; } = -1;
        public long PendingStopAtMs { get; set; }
        public long NextWanderAtMs { get; set; }
        public long LastStatDecayAtMs { get; set; } = -1;
        public bool IsStatsDirty { get; set; }
        public bool IsSleeping { get; set; }
        public bool SleepPostureSent { get; set; }
        public RoomObjectId? FeedTargetId { get; set; }
        public long NextVocalAtMs { get; set; } = -1;
        public bool PendingSleepVocal { get; set; }
        public bool PendingWakeVocal { get; set; }

        public void ClearMovement()
        {
            TilePath.Clear();
            NextTileId = -1;
            PendingStopAtMs = 0;
            FeedTargetId = null;
        }
    }
}
