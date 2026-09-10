using System.Threading;
using System.Threading.Tasks;
using Vortex.Database.Context;
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
using Vortex.Rooms.Grains.Systems;

namespace Vortex.Rooms.Grains.Modules;

public sealed partial class RoomActionModule(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;

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

        // The row leaves the room here, not two seconds later on the write-behind tick. Six paths
        // decide an item is free-standing by asking whether room_id is null -- a trade, a wired
        // chest, a jukebox, a marketplace listing, a wired settlement -- and until the flush caught
        // up they all refused an item the client had already put in the player's hand. Picking a
        // sofa up and dragging it straight into a trade did nothing, for as long as
        // DirtyItemsTickMs.
        //
        // Conditional on the row still being this room's, so a pickup racing another room's claim
        // loses rather than overwriting it.
        await using (VortexDbContext db = await _roomGrain._dbCtxFactory.CreateDbContextAsync(ct))
        {
            int released = await RoomFurnitureLocationStore.ReleaseFromRoomAsync(
                db,
                itemId.Value,
                _roomGrain.RoomId.Value,
                pickerId.Value,
                ct
            );

            if (released == 0)
            {
                throw new VortexException(VortexErrorCodeEnum.FloorItemNotFound);
            }
        }

        await _roomGrain.ObjectModule.RemoveObjectAsync(ctx, item, ct, pickerId);

        RoomItemSnapshot snapshot = item.GetSnapshot();

        IInventoryGrain inventory = _roomGrain._grainFactory.GetInventoryGrain(snapshot.OwnerId);

        await inventory.AddFurnitureFromRoomItemSnapshotAsync(snapshot, ct);

        // Detached: the item is already in the inventory and the client already has its answer.
        // See RoomGrain.PublishDetached.
        _roomGrain.PublishDetached(
            new ItemPickedUpEvent(
                itemId.Value,
                ctx.PlayerId.Value,
                previousOwnerId.Value,
                pickerId.Value,
                _roomGrain.RoomId.Value
            )
        );

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
