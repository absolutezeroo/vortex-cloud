using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    public Task<bool> PlaceWallItemAsync(
        ActionContext ctx,
        FurnitureItemSnapshot item,
        int x,
        int y,
        Altitude z,
        int wallOffset,
        Rotation rot,
        CancellationToken ct
    );
    public Task<bool> MoveWallItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Altitude z,
        int wallOffset,
        Rotation rot,
        CancellationToken ct
    );
    public Task<RoomWallItemSnapshot?> GetWallItemSnapshotByIdAsync(
        RoomObjectId itemId,
        CancellationToken ct
    );
    public Task<ImmutableArray<RoomWallItemSnapshot>> GetAllWallItemSnapshotsAsync(
        CancellationToken ct
    );
}
