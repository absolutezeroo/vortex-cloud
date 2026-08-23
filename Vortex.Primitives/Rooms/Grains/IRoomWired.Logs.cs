using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomWired
{
    public Task<WiredRoomLogPageSnapshot> GetWiredRoomLogsPageAsync(
        int page,
        int pageSize,
        int logLevelFilter,
        int logSourceFilter,
        string query,
        CancellationToken ct
    );
}
