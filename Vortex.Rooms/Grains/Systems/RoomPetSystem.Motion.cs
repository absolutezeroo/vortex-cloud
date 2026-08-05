using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Pets;
using Vortex.Logging;
using Vortex.Primitives;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Outgoing.Inventory.Pets;
using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Messages.Outgoing.Room.Pets;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Avatars;
using Vortex.Rooms.Object.Logic.Furniture.Floor;

namespace Vortex.Rooms.Grains.Systems;

public sealed partial class RoomPetSystem
{
    public async Task<PetFeedResult> FeedPetAsync(
        ActionContext ctx,
        int petId,
        RoomObjectId foodItemId,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _roomGrain
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
                bool ate = await AutoFeedPetAtBowlAsync(pet, feedId, ct).ConfigureAwait(false);
                PetSnapshot petAfterFeed = _roomGrain._state.PetsById.TryGetValue(
                    pet.PetId,
                    out PetSnapshot? fed
                )
                    ? fed
                    : pet;

                if (ate)
                {
                    return await ToAvatarSnapshotAsync(
                            petAfterFeed,
                            RoomPetRuntime.EatStatus(petAfterFeed.Z),
                            RoomPetRuntime.EatPosture,
                            ct
                        )
                        .ConfigureAwait(false);
                }

                return await ToAvatarSnapshotAsync(petAfterFeed, ct).ConfigureAwait(false);
            }

            if (motion.IsHeadingToNest)
            {
                motion.IsHeadingToNest = false;

                // Only curl up if the walk actually ended on the nest -- a path cut short by a
                // blocked tile leaves the pet standing wherever it stopped, and it should retry.
                if (IsOnNestTile(pet))
                {
                    StartNestNap(motion, now);
                }
            }

            return await ToAvatarSnapshotAsync(pet, ct).ConfigureAwait(false);
        }

        if (motion.TilePath.Count == 0 && now >= motion.NextWanderAtMs)
        {
            if (!TryDirectPetToFood(pet, motion, now) && !TryDirectPetToNest(pet, motion, now))
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
        int nutritionCap = _roomGrain._petLevelProvider.GetNutritionCapForLevel(
            pet.Type,
            pet.Level
        );
        int energyCap = _roomGrain._petLevelProvider.GetEnergyCapForLevel(pet.Type, pet.Level);

        int newNutrition = pet.Nutrition;
        int newEnergy = pet.Energy;

        int nutritionLoss = RoomPetRuntime.TakeWholeNeedPoints(
            motion.LastNutritionDecayAtMs,
            now,
            _roomGrain._roomConfig.Pet.NutritionDecayPerMinute,
            out long nextNutritionClockMs
        );
        motion.LastNutritionDecayAtMs = nextNutritionClockMs;

        if (nutritionLoss > 0)
        {
            newNutrition = Math.Clamp(pet.Nutrition - nutritionLoss, 0, nutritionCap);
        }

        if (motion.IsSleeping)
        {
            double nestMultiplier = IsOnNestTile(pet)
                ? _roomGrain._roomConfig.Pet.NestEnergyMultiplier
                : 1.0;
            int energyGain = RoomPetRuntime.TakeWholeNeedPoints(
                motion.LastEnergyDecayAtMs,
                now,
                _roomGrain._roomConfig.Pet.EnergyDecayPerMinute * 2 * nestMultiplier,
                out long nextEnergyClockMs
            );
            motion.LastEnergyDecayAtMs = nextEnergyClockMs;

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
            int energyLoss = RoomPetRuntime.TakeWholeNeedPoints(
                motion.LastEnergyDecayAtMs,
                now,
                _roomGrain._roomConfig.Pet.EnergyDecayPerMinute,
                out long nextEnergyClockMs
            );
            motion.LastEnergyDecayAtMs = nextEnergyClockMs;

            if (energyLoss > 0)
            {
                newEnergy = Math.Clamp(pet.Energy - energyLoss, 0, energyCap);
            }

            if (newEnergy == 0)
            {
                motion.IsSleeping = true;
                motion.SleepPostureSent = false;
                motion.PendingSleepVocal = true;
                motion.ClearMovement();
            }
        }

        // Mood runs on its own clock too, draining while the pet is up and paying back while it
        // rests, so a pet left alone slowly sulks and a nap cheers it up.
        int happinessCap = _roomGrain._roomConfig.Pet.HappinessCap;
        int happinessStep = RoomPetRuntime.TakeWholeNeedPoints(
            motion.LastHappinessDecayAtMs,
            now,
            motion.IsSleeping
                ? _roomGrain._roomConfig.Pet.HappinessRestGainPerMinute
                : _roomGrain._roomConfig.Pet.HappinessDecayPerMinute,
            out long nextHappinessClockMs
        );
        motion.LastHappinessDecayAtMs = nextHappinessClockMs;

        int newHappiness = pet.Happiness;

        if (happinessStep > 0)
        {
            newHappiness = Math.Clamp(
                motion.IsSleeping ? pet.Happiness + happinessStep : pet.Happiness - happinessStep,
                0,
                happinessCap
            );
        }

        if (
            newNutrition == pet.Nutrition
            && newEnergy == pet.Energy
            && newHappiness == pet.Happiness
        )
        {
            return pet;
        }

        motion.IsStatsDirty = true;

        PetSnapshot updated = pet with
        {
            Nutrition = newNutrition,
            Energy = newEnergy,
            Happiness = newHappiness,
        };
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
        // Mood ages with the rest of it, or a pet left for a week comes back starving and delighted.
        entity.Happiness = Math.Max(
            0,
            entity.Happiness
                - (int)(elapsedMinutes * _roomGrain._roomConfig.Pet.HappinessDecayPerMinute)
        );
    }

    private async Task SyncLiveStatsToPetEntityAsync(
        VortexDbContext dbCtx,
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
        entity.Happiness = live.Happiness;
        entity.RespectTodayCount = live.RespectTodayCount;
        entity.RespectLastResetDate = live.RespectLastResetDate;
        entity.CanBreed = live.CanBreed;
    }

    private bool IsOnNestTile(PetSnapshot pet)
    {
        foreach (IRoomItem item in _roomGrain._state.ItemsById.Values)
        {
            if (item.X == pet.X && item.Y == pet.Y && item.Logic is FurniturePetNestLogic)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A tired pet walks to the nearest reachable nest and naps there.
    /// </summary>
    /// <remarks>
    /// Nothing used to take a pet to its nest. It slept where it stood, only once energy had hit
    /// zero, and <see cref="IsOnNestTile" />'s recovery bonus applied only if it happened to be
    /// standing on a nest at that moment.
    /// </remarks>
    private bool TryDirectPetToNest(PetSnapshot pet, PetMotionState motion, long now)
    {
        if (!RoomPetRuntime.IsTired(pet, _roomGrain._roomConfig.Pet.TiredEnergyThreshold))
        {
            return false;
        }

        if (IsOnNestTile(pet))
        {
            StartNestNap(motion, now);

            return true;
        }

        (int X, int Y)? nest = RoomPetRuntime.PickNearestTile(
            pet.X,
            pet.Y,
            _roomGrain
                ._state.ItemsById.Values.Where(item => item.Logic is FurniturePetNestLogic)
                .Select(item => (item.X, item.Y))
        );

        if (nest is null)
        {
            return false;
        }

        (int nestX, int nestY) = nest.Value;

        IReadOnlyList<(int X, int Y)> path = _roomGrain.PathingSystem.FindPath(
            (pet.X, pet.Y),
            (nestX, nestY),
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
        motion.IsHeadingToNest = true;
        motion.NextWanderAtMs = ScheduleNextWanderAt(now);

        return true;
    }

    private void StartNestNap(PetMotionState motion, long now)
    {
        motion.ClearMovement();
        motion.IsSleeping = true;
        motion.SleepPostureSent = false;
        motion.PendingSleepVocal = true;
        motion.NextWanderAtMs = ScheduleNextWanderAt(now);
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
            bool isFood = needsFood && item.Logic is FurniturePetProductLogic;
            bool isDrink = needsDrink && item.Logic is FurniturePetDrinkLogic;

            if (!isFood && !isDrink)
            {
                continue;
            }

            if (!RoomPetRuntime.HasServingsLeft(item.Definition.TotalStates, item.Logic.GetState()))
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

    private async Task<bool> AutoFeedPetAtBowlAsync(
        PetSnapshot pet,
        RoomObjectId feedItemId,
        CancellationToken ct
    )
    {
        if (!_roomGrain._state.ItemsById.TryGetValue(feedItemId, out IRoomItem? item))
        {
            return false;
        }

        if (item.X != pet.X || item.Y != pet.Y)
        {
            return false;
        }

        bool isDrink = item.Logic is FurniturePetDrinkLogic;

        ActionContext ctx = ActionContext.CreateForPlayer(pet.OwnerId, _roomGrain.RoomId);
        PetFeedResult result = await FeedPetAsync(ctx, pet.PetId, feedItemId, ct)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return false;
        }

        if (_roomGrain._state.PetsById.TryGetValue(pet.PetId, out PetSnapshot? updated))
        {
            // The vocal still tells food from water even though the posture cannot: a pet has one
            // animation for both.
            string eatVocal = isDrink ? "DRINKING" : "EATING";
            await BroadcastPetVocalAsync(updated, eatVocal).ConfigureAwait(false);
        }

        return true;
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
}
