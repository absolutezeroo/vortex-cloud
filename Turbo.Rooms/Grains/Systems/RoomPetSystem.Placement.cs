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

        // Position is applied to the in-memory entity but not saved here — it rides the
        // periodic dirty-pet flush (FlushDirtyPetsAsync) alongside stats, same as furniture
        // position uses the timer-flush pattern. Nothing reads the DB row before that flush.
        pet.X = x;
        pet.Y = y;
        pet.Z = z;
        pet.Direction = (int)direction;

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
        GetMotionState(snapshot, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).IsStatsDirty =
            true;

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
}
