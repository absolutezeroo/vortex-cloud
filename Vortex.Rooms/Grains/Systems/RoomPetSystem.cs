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

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
///     Loading, the per-tick loop, dirty-stat flush, and shared helpers (avatar-snapshot
///     building, composer fan-out, motion-state bookkeeping). Placement/movement lives in
///     <c>RoomPetSystem.Placement.cs</c>, wander/decay/feeding AI in
///     <c>RoomPetSystem.Motion.cs</c>, respect/commands/XP in <c>RoomPetSystem.Care.cs</c>,
///     breeding and monsterplant in <c>RoomPetSystem.Breeding.cs</c>.
/// </summary>
public sealed partial class RoomPetSystem(RoomGrain roomGrain)
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

    public async Task FlushDirtyPetsAsync(CancellationToken ct)
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
            entity.X = snapshot.X;
            entity.Y = snapshot.Y;
            entity.Z = snapshot.Z;
            entity.Direction = (int)snapshot.Direction;

            if (_motionByPetId.TryGetValue(entity.Id, out PetMotionState? motion))
            {
                motion.IsStatsDirty = false;
            }
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
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
