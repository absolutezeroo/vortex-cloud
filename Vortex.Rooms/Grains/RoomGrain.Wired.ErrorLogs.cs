using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task<List<WiredErrorLogSnapshot>> GetWiredErrorLogsAsync(CancellationToken ct)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        List<WiredErrorLogSnapshot> entries = _state
            .WiredErrorLogCounters.Values.Select(
                (counter, index) =>
                    new WiredErrorLogSnapshot
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
