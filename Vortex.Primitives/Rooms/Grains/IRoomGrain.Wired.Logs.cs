using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    public Task<WiredRoomLogsComposer> GetWiredRoomLogsPageAsync(
        int page,
        int pageSize,
        int logLevelFilter,
        int logSourceFilter,
        string query,
        CancellationToken ct
    );
}
