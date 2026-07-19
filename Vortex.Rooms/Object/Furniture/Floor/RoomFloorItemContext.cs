using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Mapping;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Object.Furniture.Floor;

public sealed class RoomFloorItemContext(RoomGrain roomGrain, IRoomFloorItem roomObject)
    : RoomItemContext<IRoomFloorItem, IFurnitureFloorLogic, IRoomFloorItemContext>(
        roomGrain,
        roomObject
    ),
        IRoomFloorItemContext
{
    public int GetTileIdx() => _roomGrain.ToIdx(RoomObject.X, RoomObject.Y);

    public int GetTileIdx(int x, int y) => _roomGrain.ToIdx(x, y);

    public void RefreshTile() => _roomGrain.ComputeTile(RoomObject.X, RoomObject.Y);

    public Task<RoomTileSnapshot> GetTileSnapshotAsync(CancellationToken ct) =>
        _roomGrain.GetTileSnapshotAsync(RoomObject.X, RoomObject.Y, ct);
}
