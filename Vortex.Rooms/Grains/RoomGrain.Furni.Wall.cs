using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Action;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public async Task<bool> PlaceWallItemAsync(
        ActionContext ctx,
        FurnitureItemSnapshot item,
        int x,
        int y,
        Altitude z,
        int wallOffset,
        Rotation rot,
        CancellationToken ct
    )
    {
        try
        {
            if (!await ActionModule.PlaceWallItemAsync(ctx, item, x, y, z, wallOffset, rot, ct))
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to place wall item {ItemId} in room {RoomId}",
                item.ItemId,
                _state.RoomId
            );

            return false;
        }
    }

    public async Task<bool> MoveWallItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int newX,
        int newY,
        Altitude newZ,
        int wallOffset,
        Rotation newRot,
        CancellationToken ct
    )
    {
        try
        {
            if (
                !await ActionModule.MoveWallItemByIdAsync(
                    ctx,
                    itemId,
                    newX,
                    newY,
                    newZ,
                    wallOffset,
                    newRot,
                    ct
                )
            )
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to move wall item {ItemId} in room {RoomId}",
                itemId,
                _state.RoomId
            );

            return false;
        }
    }

    public Task<RoomWallItemSnapshot?> GetWallItemSnapshotByIdAsync(
        RoomObjectId itemId,
        CancellationToken ct
    ) =>
        Task.FromResult(
            _state.ItemsById.TryGetValue(itemId, out IRoomItem? item)
                ? item is IRoomWallItem wall
                    ? wall.GetSnapshot()
                    : null
                : null
        );

    public Task<ImmutableArray<RoomWallItemSnapshot>> GetAllWallItemSnapshotsAsync(
        CancellationToken ct
    ) => FurniModule.GetAllWallItemSnapshotsAsync(ct);
}
