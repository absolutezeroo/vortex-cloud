using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Logging;
using Vortex.Primitives;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;

namespace Vortex.Rooms.Grains.Modules;

public sealed partial class RoomActionModule
{
    public async Task<bool> PlaceWallItemAsync(
        ActionContext ctx,
        FurnitureItemSnapshot snapshot,
        int x,
        int y,
        Altitude z,
        int wallOffset,
        Rotation rot,
        CancellationToken ct
    )
    {
        if (!await _roomGrain.SecurityModule.CanPlaceFurniAsync(ctx))
        {
            throw new VortexException(VortexErrorCodeEnum.NoPermissionToPlaceFurni);
        }

        IRoomItem item = _roomGrain._itemsLoader.CreateFromFurnitureItemSnapshot(snapshot);

        if (item is not IRoomWallItem wallItem)
        {
            throw new VortexException(VortexErrorCodeEnum.WallItemNotFound);
        }

        if (
            !await _roomGrain.FurniModule.ValidateNewWallItemPlacementAsync(
                ctx,
                wallItem,
                x,
                y,
                z,
                wallOffset,
                rot
            )
        )
        {
            throw new VortexException(VortexErrorCodeEnum.InvalidMoveTarget);
        }

        if (
            !await _roomGrain.FurniModule.PlaceWallItemAsync(
                ctx,
                wallItem,
                x,
                y,
                z,
                wallOffset,
                rot,
                ct
            )
        )
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
                            z = z.ToString(),
                            wallOffset,
                            rotation = rot.ToString(),
                        }
                    ),
                    snapshot.Definition.Id
                ),
                ct
            )
            .ConfigureAwait(false);

        return true;
    }

    public async Task<bool> MoveWallItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Altitude z,
        int wallOffset,
        Rotation rot,
        CancellationToken ct
    )
    {
        if (!await _roomGrain.SecurityModule.CanManipulateFurniAsync(ctx))
        {
            throw new VortexException(VortexErrorCodeEnum.NoPermissionToManipulateFurni);
        }

        if (
            !await _roomGrain.FurniModule.ValidateWallItemPlacementAsync(
                ctx,
                itemId,
                x,
                y,
                z,
                wallOffset,
                rot
            )
        )
        {
            throw new VortexException(VortexErrorCodeEnum.InvalidMoveTarget);
        }

        if (
            !await _roomGrain.FurniModule.MoveWallItemByIdAsync(
                ctx,
                itemId,
                x,
                y,
                z,
                wallOffset,
                rot,
                ct
            )
        )
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
                            z = z.ToString(),
                            wallOffset,
                            rotation = rot.ToString(),
                        }
                    )
                ),
                ct
            )
            .ConfigureAwait(false);

        return true;
    }
}
