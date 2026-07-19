using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Inventory.Furniture;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Primitives.Inventory.Factories;

public interface IInventoryFurnitureLoader
{
    public Task<IReadOnlyList<IFurnitureItem>> LoadByPlayerIdAsync(
        PlayerId playerId,
        CancellationToken ct
    );
    public IFurnitureItem CreateFromRoomItemSnapshot(RoomItemSnapshot snapshot);
}
