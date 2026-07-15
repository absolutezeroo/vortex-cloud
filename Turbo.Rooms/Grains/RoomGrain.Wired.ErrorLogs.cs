using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

namespace Turbo.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task<List<WiredErrorLogEntry>> GetWiredErrorLogsAsync(CancellationToken ct)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        List<WiredErrorLogEntry> entries = _state
            .WiredErrorLogCounters.Values.Select(
                (counter, index) =>
                    new WiredErrorLogEntry
                    {
                        ErrorId = index,
                        ErrorName = counter.ErrorName,
                        Category = counter.Category,
                        ThrowCount = counter.ThrowCount,
                        MsSinceLastOccurrence = now - counter.LastOccurrenceMs,
                    }
            )
            .ToList();

        return Task.FromResult(entries);
    }

    public Task ClearWiredErrorLogsAsync(CancellationToken ct)
    {
        _state.WiredErrorLogCounters.Clear();

        return Task.CompletedTask;
    }
}
