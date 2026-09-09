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

    /// <remarks>
    /// The item's whole footprint, not the single tile its coordinates name. Every floor logic
    /// reaches this through <c>OnStateChangedAsync</c>, so a state change that alters height or
    /// walkability used to refresh one tile of a 2x2 and leave the other three answering for the
    /// state before it.
    /// </remarks>
    public void RefreshTile() => _roomGrain.MapModule.RecomputeFootprint(RoomObject);

    public Task<RoomTileSnapshot> GetTileSnapshotAsync(CancellationToken ct) =>
        _roomGrain.GetTileSnapshotAsync(RoomObject.X, RoomObject.Y, ct);
}
