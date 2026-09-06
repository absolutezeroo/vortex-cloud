using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Benchmark;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Starting and stopping a load run.
/// </summary>
/// <remarks>
/// Audited like every other operation, and for once the audit is the point rather than a formality:
/// a run makes the hotel slow for everyone in it, so "who did this, when, and why" is the first
/// question anyone will ask about the ten minutes it was happening.
/// </remarks>
internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> StartBenchmarkAsync(
        BenchmarkStartRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.benchmark.start",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.Players,
                request.Furniture,
                request.FurnitureIds,
                request.RoomId,
                request.DurationSeconds,
                request.Label,
            },
            work: async c =>
            {
                BenchmarkStartResult result = await _benchmark
                    .StartAsync(
                        new BenchmarkPlan
                        {
                            Players = request.Players,
                            Furniture = request.Furniture,
                            FurnitureDefinitionIds = [.. request.FurnitureIds ?? []],
                            RoomId = request.RoomId,
                            DurationSeconds = request.DurationSeconds,
                            RampSeconds = request.RampSeconds,
                            WalkIntervalMs = request.WalkIntervalMs,
                            ChatIntervalMs = request.ChatIntervalMs,
                            Label = request.Label ?? string.Empty,
                        },
                        c
                    )
                    .ConfigureAwait(false);

                if (!result.Started)
                {
                    throw new InvalidOperationException(result.ErrorCode ?? "benchmark_refused");
                }
            },
            ct
        );

    public Task<OperationResult> StopBenchmarkAsync(
        BenchmarkStopRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.benchmark.stop",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { },
            work: c => _benchmark.StopAsync(c),
            ct
        );
}
