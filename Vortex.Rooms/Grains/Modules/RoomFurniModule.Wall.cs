using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Logging;
using Vortex.Primitives;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Rooms.Grains.Modules;

public sealed partial class RoomFurniModule
{
    public async Task<bool> PlaceWallItemAsync(
        ActionContext ctx,
        IRoomWallItem item,
        int x,
        int y,
        Altitude z,
        int wallOffset,
        Rotation rot,
        CancellationToken ct
    )
    {
        if (
            !await _roomGrain.ObjectModule.AttatchObjectAsync(item, ct)
            || !_roomGrain.MapModule.PlaceWallItem(item, x, y, z, rot, wallOffset)
        )
        {
            return false;
        }

        await item.Logic.OnPlaceAsync(ctx, ct);

        item.MarkDirty();

        await _roomGrain.SendComposerToRoomAsync(item.GetAddComposer());

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
        if (
            !_roomGrain._state.ItemsById.TryGetValue(itemId, out IRoomItem? item)
            || item is not IRoomWallItem wall
        )
        {
            throw new VortexException(VortexErrorCodeEnum.WallItemNotFound);
        }

        if (!_roomGrain.MapModule.MoveWallItem(wall, x, y, z, rot, wallOffset))
        {
            return false;
        }

        await _roomGrain.SendComposerToRoomAsync(item.GetUpdateComposer());

        await item.Logic.OnMoveAsync(ctx, -1, ct);

        return true;
    }

    public Task<bool> ValidateWallItemPlacementAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Altitude z,
        int wallOffset,
        Rotation rot
    ) => Task.FromResult(true);

    public Task<bool> ValidateNewWallItemPlacementAsync(
        ActionContext ctx,
        IRoomWallItem item,
        int x,
        int y,
        Altitude z,
        int wallOffset,
        Rotation rot
    ) => Task.FromResult(true);

    public Task<ImmutableArray<RoomWallItemSnapshot>> GetAllWallItemSnapshotsAsync(
        CancellationToken ct
    ) =>
        Task.FromResult(
            _roomGrain
                ._state.ItemsById.Values.OfType<IRoomWallItem>()
                .Select(x => x.GetSnapshot())
                .ToImmutableArray()
        );
}
