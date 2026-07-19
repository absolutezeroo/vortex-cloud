using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    public Task<List<WiredErrorLogEntry>> GetWiredErrorLogsAsync(CancellationToken ct);

    public Task ClearWiredErrorLogsAsync(CancellationToken ct);
}
