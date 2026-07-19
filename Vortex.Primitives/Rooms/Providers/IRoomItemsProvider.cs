using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;

namespace Vortex.Primitives.Rooms.Providers;

public interface IRoomItemsProvider
{
    public Task<(
        IReadOnlyList<IRoomFloorItem>,
        IReadOnlyList<IRoomWallItem>,
        IReadOnlyDictionary<PlayerId, string>
    )> LoadByRoomIdAsync(RoomId roomId, CancellationToken ct);
    public IRoomItem CreateFromFurnitureItemSnapshot(FurnitureItemSnapshot item);
}
