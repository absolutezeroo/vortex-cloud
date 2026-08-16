using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Benchmark;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// What the last load run measured, and what the current one is doing.
/// </summary>
/// <remarks>
/// <para>
/// Read straight off the running service rather than out of a table: a run's samples exist for the
/// length of the run and the reading of it, and writing a row a second for something nobody queries
/// afterwards would be the "unbounded growth for data nobody reads" mistake the client-performance
/// telemetry already had to be walked back from.
/// </para>
/// <para>
/// The server half of the answer comes from here. The client half — frame rate — arrives on its own
/// path: every connected client reports it periodically, and the page reads it from the room
/// performance surface. Synthetic players cannot supply it, having nothing to draw.
/// </para>
/// </remarks>
internal sealed partial class DashboardApiService
{
    public async Task<object> BenchmarkAsync(CancellationToken ct)
    {
        await _benchmark.ReadEnabledAsync().ConfigureAwait(false);

        BenchmarkStatus status = _benchmark.GetStatus();

        var samples = status
            .Samples.Select(sample => new
            {
                at = sample.AtUtc,
                sample.ConnectedClients,
                sample.RttMedianMs,
                sample.RttP95Ms,
                sample.PacketsReceived,
                sample.BytesReceived,
                sample.Failures,
            })
            .ToList();

        return new
        {
            phase = status.Phase.ToString(),
            running = status.Phase
                is BenchmarkPhase.Provisioning
                    or BenchmarkPhase.Ramping
                    or BenchmarkPhase.Steady
                    or BenchmarkPhase.TearingDown,
            plan = status.Plan is null
                ? null
                : new
                {
                    status.Plan.Players,
                    status.Plan.Furniture,
                    status.Plan.DurationSeconds,
                    status.Plan.RampSeconds,
                    status.Plan.WalkIntervalMs,
                    status.Plan.ChatIntervalMs,
                    status.Plan.Label,
                },
            startedAt = status.StartedAtUtc,
            endedAt = status.EndedAtUtc,
            status.ConnectedClients,
            status.PlacedFurniture,
            status.RoomId,
            status.Enabled,
            status.BorrowedRoom,
            status.Error,
            // Non-null means rows were left in the hotel. Surfaced rather than logged: it is the
            // one outcome of a run that outlives the run.
            status.Residue,
            // The artefact. A number on a page cannot be attached to anything; this can.
            status.ReportPath,
            samples,
            summary = samples.Count == 0
                ? null
                : new
                {
                    peakClients = samples.Max(s => s.ConnectedClients),
                    // Taken over the whole run rather than the last sample: the tail is what a
                    // player notices, and it does not show up in an average.
                    worstRttMs = samples.Max(s => s.RttP95Ms),
                    medianRttMs = Median(samples.Select(s => s.RttMedianMs).ToList()),
                    totalPackets = samples.Count == 0 ? 0 : samples[^1].PacketsReceived,
                    totalBytes = samples.Count == 0 ? 0 : samples[^1].BytesReceived,
                    failures = samples.Count == 0 ? 0 : samples[^1].Failures,
                },
        };
    }

    private static double Median(System.Collections.Generic.List<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        values.Sort();

        return values[values.Count / 2];
    }
}
