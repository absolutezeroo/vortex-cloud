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

public sealed partial class RoomPetSystem
{
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
}
