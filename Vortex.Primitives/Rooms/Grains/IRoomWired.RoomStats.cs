using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomWired
{
    public Task<WiredRoomStatsSnapshot> GetWiredRoomStatsAsync(CancellationToken ct);
}
