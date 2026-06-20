using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Database.Entities.Pets;
using Turbo.Logging;
using Turbo.Primitives;
using Turbo.Primitives.Action;
using Turbo.Primitives.Pets.Snapshots;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Furniture;

namespace Turbo.Rooms.Grains.Systems;

public sealed class RoomPetSystem(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;

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

        foreach (PetEntity pet in pets)
        {
            _roomGrain._state.PetsById[pet.Id] = RoomPetRuntime.ToSnapshot(pet);
        }

        _roomGrain._state.IsPetsLoaded = true;
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
        if (!_roomGrain._state.RoomSnapshot.AllowPets)
        {
            return null;
        }

        await EnsureRoomReadyForPetPlacementAsync(ct).ConfigureAwait(false);

        double z = GetTileHeightForPet(x, y);

        await using TurboDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PetEntity? pet = await dbCtx
            .Pets.SingleOrDefaultAsync(
                p => p.Id == petId && p.RoomEntityId == null && p.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (pet is null)
        {
            return null;
        }

        if (pet.OwnerPlayerEntityId != ctx.PlayerId)
        {
            throw new TurboException(TurboErrorCodeEnum.NoPermissionToManipulatePet);
        }

        pet.RoomEntityId = _roomGrain.RoomId.Value;
        pet.X = x;
        pet.Y = y;
        pet.Z = z;
        pet.Direction = (int)direction;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        PetSnapshot snapshot = RoomPetRuntime.ToSnapshot(pet);
        _roomGrain._state.PetsById[pet.Id] = snapshot;

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
        _roomGrain._state.PetsById[pet.Id] = snapshot;

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

        pet.RoomEntityId = null;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        _roomGrain._state.PetsById.Remove(pet.Id);

        return RoomPetRuntime.ToSnapshot(pet);
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

        PetFeedResult result = await RoomPetRuntime
            .FeedAsync(
                dbCtx,
                _roomGrain.RoomId.Value,
                ctx.PlayerId.Value,
                petId,
                foodItemId,
                _roomGrain._state.RoomSnapshot.AllowPetsEat,
                ct
            )
            .ConfigureAwait(false);

        if (!result.Success || result.Pet is null)
        {
            return result;
        }

        _roomGrain._state.PetsById[petId] = result.Pet;

        await RemoveConsumedFoodFromLiveStateAsync(ctx, foodItemId, ct).ConfigureAwait(false);

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

    private async Task<PetEntity?> LoadPlacedPetAsync(
        TurboDbContext dbCtx,
        int petId,
        CancellationToken ct
    ) =>
        await dbCtx
            .Pets.SingleOrDefaultAsync(
                p =>
                    p.Id == petId
                    && p.RoomEntityId == _roomGrain.RoomId.Value
                    && p.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

    private static void EnsurePetOwner(ActionContext ctx, PetEntity pet)
    {
        if (pet.OwnerPlayerEntityId != ctx.PlayerId)
        {
            throw new TurboException(TurboErrorCodeEnum.NoPermissionToManipulatePet);
        }
    }

    private async Task RemoveConsumedFoodFromLiveStateAsync(
        ActionContext ctx,
        RoomObjectId foodItemId,
        CancellationToken ct
    )
    {
        if (!_roomGrain._state.ItemsById.TryGetValue(foodItemId, out IRoomItem? item))
        {
            return;
        }

        if (!_roomGrain.MapModule.RemoveItem(item))
        {
            return;
        }

        await _roomGrain
            .SendComposerToRoomAsync(item.GetRemoveComposer(ctx.PlayerId, isExpired: true))
            .ConfigureAwait(false);

        await item.Logic.OnDetachAsync(ct).ConfigureAwait(false);
        item.SetAction(null);
        _roomGrain._state.ItemsById.Remove(foodItemId);
    }
}
