using System.Threading;
using System.Threading.Tasks;
using Vortex.Logging;
using Vortex.Primitives;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Rooms.Grains.Modules;

public sealed partial class RoomActionModule(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;

    public Task<bool> AddItemAsync(IRoomItem item, CancellationToken ct) =>
        _roomGrain.ObjectModule.AttatchObjectAsync(item, ct);

    public async Task<bool> RemoveItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct
    )
    {
        if (!_roomGrain._state.ItemsById.TryGetValue(itemId, out IRoomItem? item))
        {
            throw new VortexException(VortexErrorCodeEnum.FloorItemNotFound);
        }

        FurniturePickupType pickupType = await _roomGrain.SecurityModule.GetFurniPickupTypeAsync(
            ctx
        );

        if (pickupType == FurniturePickupType.None)
        {
            throw new VortexException(VortexErrorCodeEnum.NoPermissionToManipulateFurni);
        }

        PlayerId previousOwnerId = item.OwnerId;
        PlayerId pickerId = item.OwnerId;

        if (pickupType is FurniturePickupType.SendToCtx)
        {
            pickerId = ctx.PlayerId;
        }

        item.SetOwnerId(pickerId);

        await _roomGrain.ObjectModule.RemoveObjectAsync(ctx, item, ct, pickerId);

        RoomItemSnapshot snapshot = item.GetSnapshot();

        IInventoryGrain inventory = _roomGrain._grainFactory.GetInventoryGrain(snapshot.OwnerId);

        await inventory.AddFurnitureFromRoomItemSnapshotAsync(snapshot, ct);

        await _roomGrain
            ._events.PublishAsync(
                new ItemPickedUpEvent(
                    itemId.Value,
                    ctx.PlayerId.Value,
                    previousOwnerId.Value,
                    pickerId.Value,
                    _roomGrain.RoomId.Value
                ),
                ct
            )
            .ConfigureAwait(false);

        return true;
    }

    public async Task<bool> UseItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct,
        int param = -1
    )
    {
        if (!_roomGrain._state.ItemsById.TryGetValue(itemId, out IRoomItem? item))
        {
            throw new VortexException(VortexErrorCodeEnum.FloorItemNotFound);
        }

        FurnitureUsageType usagePolicy = item.Logic.GetUsagePolicy();

        bool canUse = await _roomGrain.SecurityModule.CanUseFurniAsync(ctx, usagePolicy);

        if (!canUse)
        {
            canUse =
                await _roomGrain.SecurityModule.FindRentedSpaceForOwnedItemAsync(ctx, itemId, ct)
                    is not null;
        }

        if (!canUse || !await _roomGrain.FurniModule.UseItemByIdAsync(ctx, itemId, ct, param))
        {
            return false;
        }

        return true;
    }

    public Task<bool> ClickItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct,
        int param = -1
    ) => _roomGrain.FurniModule.ClickItemByIdAsync(ctx, itemId, ct, param);
}
