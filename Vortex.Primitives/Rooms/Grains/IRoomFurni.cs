using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>Furniture placed in the room. Split across this file and the
/// <c>IRoomFurni.Floor</c>/<c>.Wall</c>/<c>.Edit</c> parts, which keep the same
/// per-family layout the implementation uses.</summary>
[Alias("Vortex.Primitives.Rooms.Grains.IRoomFurni")]
public partial interface IRoomFurni : IGrainWithIntegerKey
{
    public Task<bool> AddItemAsync(IRoomItem item, CancellationToken ct);
    public Task<bool> RemoveItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct
    );
    public Task<bool> UseItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct,
        int param = -1
    );
    public Task<bool> ClickItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct,
        int param = -1
    );
    public Task<ImmutableDictionary<PlayerId, string>> GetAllOwnersAsync(CancellationToken ct);
    public Task<RoomItemSnapshot?> GetItemSnapshotByIdAsync(
        RoomObjectId itemId,
        CancellationToken ct
    );

    /// <summary>
    /// Sets the logical state of a floor item and broadcasts the visual update to all
    /// room occupants. No-op if the item is not found or not in this room.
    /// </summary>
    public Task SetFloorItemStateAsync(RoomObjectId itemId, int state, CancellationToken ct);
}
