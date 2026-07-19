using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Snapshots.Mapping;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    public Task ClickTileAsync(ActionContext ctx, int x, int y, CancellationToken ct);
    public Task<RoomTileSnapshot> GetTileSnapshotAsync(int x, int y, CancellationToken ct);
    public Task<RoomTileSnapshot> GetTileSnapshotAsync(int id, CancellationToken ct);
    public Task<RoomMapSnapshot> GetMapSnapshotAsync(CancellationToken ct);
}
