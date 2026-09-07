using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Dashboard.API.Api.Platform.Contracts;
using Vortex.Database.Context;
using Vortex.Observability.Runtime;
using Vortex.Primitives.Benchmark;

namespace Vortex.Dashboard.API.Api.Platform;

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
internal sealed class BenchmarkReads(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    IBenchmarkService benchmark,
    RoomPerformanceAggregator roomPerformance
) : DashboardReads(dbContextFactory)
{
    private readonly IBenchmarkService _benchmark = benchmark;
    private readonly RoomPerformanceAggregator _roomPerformance = roomPerformance;

    public async Task<BenchmarkState> BenchmarkAsync(CancellationToken ct)
    {
        await _benchmark.ReadEnabledAsync().ConfigureAwait(false);

        BenchmarkStatus status = _benchmark.GetStatus();

        List<BenchmarkSampleView> samples = status
            .Samples.Select(sample => new BenchmarkSampleView(
                sample.AtUtc,
                sample.ConnectedClients,
                sample.RttMedianMs,
                sample.RttP95Ms,
                sample.PacketsReceived,
                sample.BytesReceived,
                sample.Failures
            ))
            .ToList();

        BenchmarkVerdict verdict = BenchmarkVerdict.Evaluate(
            status.Samples,
            _roomPerformance.GetSnapshot().Tick.P99Ms
        );

        return new BenchmarkState(
            status.Phase.ToString(),
            status.Phase
                is BenchmarkPhase.Provisioning
                    or BenchmarkPhase.Ramping
                    or BenchmarkPhase.Steady
                    or BenchmarkPhase.TearingDown,
            status.Plan is null
                ? null
                : new BenchmarkPlanView(
                    status.Plan.Players,
                    status.Plan.Furniture,
                    status.Plan.DurationSeconds,
                    status.Plan.RampSeconds,
                    status.Plan.WalkIntervalMs,
                    status.Plan.ChatIntervalMs,
                    status.Plan.Label
                ),
            status.StartedAtUtc,
            status.EndedAtUtc,
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
            // The same judgement the report stores, so the page and the file always agree. The tick
            // figure comes from the live aggregator: for a finished run it still covers its window.
            new BenchmarkVerdictView(
                verdict.Grade.ToString(),
                verdict.Headline,
                [.. verdict.Findings],
                verdict.MedianRttMs,
                verdict.Stalls,
                verdict.TickBudgetPercent
            ),
            // The runs already on disk. Read from the files, so the list survives a restart -- which
            // is the whole reason a run writes one.
            _benchmark
                .ListRuns(25)
                .Select(run => new BenchmarkRunView(
                    run.FileName,
                    run.Path,
                    run.SizeBytes,
                    run.WrittenAtUtc,
                    run.Players,
                    run.Furniture,
                    run.DurationSeconds,
                    run.Label,
                    run.Phase,
                    run.RoomId,
                    run.BorrowedRoom,
                    run.PeakClients,
                    run.WorstRttMs,
                    run.Failures,
                    run.Grade,
                    run.Headline
                ))
                .ToList(),
            samples.Count == 0
                ? null
                : new BenchmarkSummaryView(
                    samples.Max(s => s.ConnectedClients),
                    // Taken over the whole run rather than the last sample: the tail is what a
                    // player notices, and it does not show up in an average.
                    samples.Max(s => s.RttP95Ms),
                    Median(samples.Select(s => s.RttMedianMs).ToList()),
                    samples[^1].PacketsReceived,
                    samples[^1].BytesReceived,
                    samples[^1].Failures
                )
        );
    }

    public Task<string?> BenchmarkRunAsync(string fileName, CancellationToken ct) =>
        _benchmark.ReadRunAsync(fileName, ct);

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
