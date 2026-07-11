using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Primitives.Action;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;

namespace Turbo.Rooms.Grains.Modules;

public sealed class RoomSecurityModule(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;

    public async Task<bool> CanManipulateFurniAsync(ActionContext ctx)
    {
        RoomControllerType controllerLevel = await GetControllerLevelAsync(ctx);

        if (controllerLevel >= RoomControllerType.GroupAdmin)
        {
            return true;
        }

        if (_roomGrain._state.RoomSnapshot.GroupId is null)
        {
            if (controllerLevel >= RoomControllerType.Rights)
            {
                return true;
            }
        }
        return false;
    }

    public async Task<bool> CanUseFurniAsync(ActionContext ctx, FurnitureUsageType usageType)
    {
        RoomControllerType controllerLevel = await GetControllerLevelAsync(ctx);

        if (usageType == FurnitureUsageType.Nobody)
        {
            return false;
        }

        if (usageType == FurnitureUsageType.Controller)
        {
            if (controllerLevel < RoomControllerType.Rights)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<bool> CanPlaceFurniAsync(ActionContext ctx)
    {
        return await CanManipulateFurniAsync(ctx);
    }

    /// <summary>
    /// Returns the rentable-space floor item if <paramref name="ctx"/> player owns
    /// <paramref name="itemId"/> and the item sits inside their active rented zone.
    /// Used to gate move/use operations for renters.
    /// </summary>
    public async Task<IRoomFloorItem?> FindRentedSpaceForOwnedItemAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct
    )
    {
        if (!_roomGrain._state.ItemsById.TryGetValue(itemId, out IRoomItem? item))
        {
            return null;
        }

        if (item.OwnerId.Value != ctx.PlayerId.Value)
        {
            return null;
        }

        IRoomFloorItem? spaceItem = await FindRentedSpaceForPlayerAsync(ctx.PlayerId.Value, ct);

        if (spaceItem is null)
        {
            return null;
        }

        if (!_roomGrain.FurniModule.GetTileIdForFloorItem(spaceItem, out List<int> spaceTileIds))
        {
            return null;
        }

        int itemTile = _roomGrain.MapModule.ToIdx(item.X, item.Y);

        return spaceTileIds.Contains(itemTile) ? spaceItem : null;
    }

    /// <summary>
    /// Returns the rentable-space floor item that <paramref name="playerId"/> is currently
    /// renting in this room, or null if none exists.
    /// </summary>
    public async Task<IRoomFloorItem?> FindRentedSpaceForPlayerAsync(
        int playerId,
        CancellationToken ct
    )
    {
        if (playerId <= 0)
        {
            return null;
        }

        DateTime now = DateTime.UtcNow;
        int roomId = _roomGrain._state.RoomId.Value;

        await using TurboDbContext db = await _roomGrain._dbCtxFactory.CreateDbContextAsync(ct);

        int furnitureId = await (
            from rrs in db.RoomRentableSpaces.AsNoTracking()
            join f in db.Furnitures.AsNoTracking() on rrs.FurnitureEntityId equals f.Id
            where
                rrs.RenterPlayerEntityId == playerId
                && rrs.DeletedAt == null
                && rrs.RentedUntil > now
                && f.RoomEntityId == roomId
                && f.DeletedAt == null
            select rrs.FurnitureEntityId
        ).FirstOrDefaultAsync(ct);

        if (furnitureId == 0)
        {
            return null;
        }

        RoomObjectId objectId = new(furnitureId);

        return _roomGrain._state.ItemsById.TryGetValue(objectId, out IRoomItem? item)
            ? item as IRoomFloorItem
            : null;
    }

    public async Task<FurniturePickupType> GetFurniPickupTypeAsync(ActionContext ctx)
    {
        if (ctx.Origin == ActionOrigin.System)
        {
            return FurniturePickupType.SendToOwner;
        }

        if (await GetControllerLevelAsync(ctx) >= RoomControllerType.GroupAdmin)
        {
            return FurniturePickupType.SendToOwner;
        }

        return FurniturePickupType.None;
    }

    public async Task<bool> GetIsRoomOwnerAsync(ActionContext ctx)
    {
        bool isExplicitOwner = _roomGrain._state.RoomSnapshot.OwnerId == ctx.PlayerId;

        PermissionSet permissions = await ResolvePermissionsAsync(ctx);

        return RoomSecurityPolicy.IsRoomOwner(permissions, isExplicitOwner);
    }

    public async Task<RoomControllerType> GetControllerLevelAsync(ActionContext ctx)
    {
        if (ctx.Origin == ActionOrigin.System)
        {
            return RoomControllerType.Moderator;
        }

        PermissionSet permissions = await ResolvePermissionsAsync(ctx);

        bool isExplicitOwner = _roomGrain._state.RoomSnapshot.OwnerId == ctx.PlayerId;
        bool hasExplicitRights = _roomGrain._state.PlayerIdsWithRights.Contains(ctx.PlayerId);

        return RoomSecurityPolicy.ResolveControllerLevel(
            ctx.Origin,
            permissions,
            isExplicitOwner,
            hasExplicitRights
        );
    }

    private Task<PermissionSet> ResolvePermissionsAsync(ActionContext ctx)
    {
        if (ctx.PlayerId == PlayerId.Invalid)
        {
            return Task.FromResult(PermissionSet.Empty);
        }

        return _roomGrain._permissionService.ResolveForPlayerAsync(ctx.PlayerId);
    }

    public async Task RefreshControllerLevelForPlayerAsync(PlayerId playerId, CancellationToken ct)
    {
        RoomControllerType controllerLevel = await GetControllerLevelAsync(
            new ActionContext { PlayerId = playerId, Origin = ActionOrigin.Player }
        );

        await _roomGrain._grainFactory
            .GetPlayerPresenceGrain(playerId)
            .OnControllerLevelUpdatedAsync(_roomGrain.RoomId, controllerLevel, ct)
            .ConfigureAwait(true);
    }
}
