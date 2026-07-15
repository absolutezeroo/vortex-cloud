using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

namespace Turbo.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    public Task<List<WiredErrorLogEntry>> GetWiredErrorLogsAsync(CancellationToken ct);

    public Task ClearWiredErrorLogsAsync(CancellationToken ct);
}
