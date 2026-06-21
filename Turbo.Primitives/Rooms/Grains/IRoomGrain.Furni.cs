using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Action;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Snapshots.Furniture;

namespace Turbo.Primitives.Rooms.Grains;

public partial interface IRoomGrain
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
