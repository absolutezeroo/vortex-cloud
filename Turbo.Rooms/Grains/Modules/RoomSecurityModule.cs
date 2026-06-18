using System.Threading.Tasks;
using Turbo.Primitives.Action;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.Rooms.Grains.Modules;

public sealed class RoomSecurityModule(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;

    public async Task<bool> CanManipulateFurniAsync(ActionContext ctx)
    {
        var controllerLevel = await GetControllerLevelAsync(ctx);

        if (controllerLevel >= RoomControllerType.GroupAdmin)
            return true;

        var isGroupRoom = false;

        if (isGroupRoom)
        {
            var canGroupDecorate = false;

            if (controllerLevel >= RoomControllerType.GroupRights && canGroupDecorate)
                return true;
        }
        else
        {
            if (controllerLevel >= RoomControllerType.Rights)
                return true;
        }
        return false;
    }

    public async Task<bool> CanUseFurniAsync(ActionContext ctx, FurnitureUsageType usageType)
    {
        var controllerLevel = await GetControllerLevelAsync(ctx);

        if (usageType == FurnitureUsageType.Nobody)
            return false;

        if (usageType == FurnitureUsageType.Controller)
        {
            if (controllerLevel < RoomControllerType.Rights)
                return false;
        }

        return true;
    }

    public async Task<bool> CanPlaceFurniAsync(ActionContext ctx)
    {
        // TODO placement rules?

        return await CanManipulateFurniAsync(ctx);
    }

    public async Task<FurniturePickupType> GetFurniPickupTypeAsync(ActionContext ctx)
    {
        if (ctx.Origin == ActionOrigin.System)
            return FurniturePickupType.SendToOwner;

        // if can steal furni, SendToRequester

        if (await GetControllerLevelAsync(ctx) >= RoomControllerType.GroupAdmin)
            return FurniturePickupType.SendToOwner;

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
            return RoomControllerType.Moderator;

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
            return Task.FromResult(PermissionSet.Empty);

        return _roomGrain._permissionService.ResolveForPlayerAsync(ctx.PlayerId);
    }
}
