using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Logging;
using Turbo.Primitives;
using Turbo.Primitives.Action;
using Turbo.Primitives.Events;
using Turbo.Primitives.Inventory.Grains;
using Turbo.Primitives.Inventory.Snapshots;
using Turbo.Primitives.Messages.Incoming.Userdefinedroomevents;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Rooms.Object.Logic.Furniture.Floor.Wired;

namespace Turbo.Rooms.Grains.Modules;

public sealed partial class RoomActionModule
{
    public async Task<bool> PlaceFloorItemAsync(
        ActionContext ctx,
        FurnitureItemSnapshot snapshot,
        int x,
        int y,
        Rotation rot,
        CancellationToken ct
    )
    {
        if (!await _roomGrain.SecurityModule.CanPlaceFurniAsync(ctx))
        {
            throw new TurboException(TurboErrorCodeEnum.NoPermissionToPlaceFurni);
        }

        IRoomItem item = _roomGrain._itemsLoader.CreateFromFurnitureItemSnapshot(snapshot);

        if (item is not IRoomFloorItem floorItem)
        {
            throw new TurboException(TurboErrorCodeEnum.FloorItemNotFound);
        }

        if (
            !await _roomGrain.FurniModule.ValidateNewFloorItemPlacementAsync(
                ctx,
                floorItem,
                x,
                y,
                rot
            )
        )
        {
            throw new TurboException(TurboErrorCodeEnum.InvalidMoveTarget);
        }

        if (!await _roomGrain.FurniModule.PlaceFloorItemAsync(ctx, floorItem, x, y, rot, ct))
        {
            return false;
        }

        IInventoryGrain inventory = _roomGrain._grainFactory.GetInventoryGrain(item.OwnerId);

        await inventory.RemoveFurnitureAsync(item.ObjectId, ct);

        await _roomGrain
            ._events.PublishAsync(
                new ItemPlacedEvent(
                    item.ObjectId.Value,
                    ctx.PlayerId.Value,
                    item.OwnerId.Value,
                    _roomGrain.RoomId.Value,
                    JsonSerializer.Serialize(
                        new
                        {
                            x,
                            y,
                            rotation = rot.ToString(),
                        }
                    )
                ),
                ct
            )
            .ConfigureAwait(false);

        return true;
    }

    public async Task<bool> MoveFloorItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Rotation rot,
        CancellationToken ct
    )
    {
        if (!await _roomGrain.SecurityModule.CanManipulateFurniAsync(ctx))
        {
            throw new TurboException(TurboErrorCodeEnum.NoPermissionToManipulateFurni);
        }

        if (!await _roomGrain.FurniModule.ValidateFloorItemPlacementAsync(ctx, itemId, x, y, rot))
        {
            throw new TurboException(TurboErrorCodeEnum.InvalidMoveTarget);
        }

        if (!await _roomGrain.FurniModule.MoveFloorItemByIdAsync(ctx, itemId, x, y, null, rot, ct))
        {
            return false;
        }

        await _roomGrain
            ._events.PublishAsync(
                new ItemMovedEvent(
                    itemId.Value,
                    ctx.PlayerId.Value,
                    _roomGrain.RoomId.Value,
                    JsonSerializer.Serialize(
                        new
                        {
                            x,
                            y,
                            rotation = rot.ToString(),
                        }
                    )
                ),
                ct
            )
            .ConfigureAwait(false);

        return true;
    }

    public async Task<bool> ApplyWiredUpdateAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        UpdateWiredMessage update,
        CancellationToken ct
    )
    {
        if (!_roomGrain._state.ItemsById.TryGetValue(itemId, out IRoomItem? item))
        {
            throw new TurboException(TurboErrorCodeEnum.FloorItemNotFound);
        }

        if (item.Logic is not FurnitureWiredLogic wiredLogic)
        {
            throw new TurboException(TurboErrorCodeEnum.FloorItemNotFound);
        }

        if (!await wiredLogic.ApplyWiredUpdateAsync(ctx, update, ct))
        {
            return false;
        }

        return true;
    }
}
