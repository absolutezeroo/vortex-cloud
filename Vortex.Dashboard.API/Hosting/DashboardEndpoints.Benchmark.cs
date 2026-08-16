using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Operations;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// The load test: what the last run measured, and the two buttons that start and stop one.
///
/// <para>
/// Reading is one capability and running is another, deliberately. The numbers are useful to anyone
/// tuning the hotel; making them is a physical act that takes the hotel down a peg while it happens,
/// and the people who should be trusted with the second are a shorter list than the first.
/// </para>
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagBenchmark = "Benchmark";
    private const string ApiBenchmark = ApiV1 + "/benchmark";

    /// <summary>
    /// What a single run may ask for. A benchmark is supposed to hurt, but only on purpose: these
    /// stop a slipped digit from opening forty thousand sockets or filling a room with a million
    /// items, either of which would take the hotel down rather than measure it.
    /// </summary>
    private const int MaxPlayers = 2000;
    private const int MaxFurniture = 20000;
    private const int MaxDurationSeconds = 3600;

    public static void MapBenchmarkReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiBenchmark,
            "/api/benchmark",
            (DashboardApiService api, CancellationToken ct) => OkAsync(api.BenchmarkAsync(ct)),
            Capabilities.Dashboard.BenchmarkRead,
            TagBenchmark
        );
    }

    public static void MapBenchmarkOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/benchmark/start",
            "/api/ops/benchmark/start",
            async (
                HttpContext ctx,
                BenchmarkStartRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                !IsSane(body)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.StartBenchmarkAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsBenchmarkRun,
            TagBenchmark
        );

        MapPost(
            app,
            ApiOperations + "/benchmark/stop",
            "/api/ops/benchmark/stop",
            async (
                HttpContext ctx,
                BenchmarkStopRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                Results.Ok(
                    await ops.StopBenchmarkAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                ),
            Capabilities.Dashboard.OpsBenchmarkRun,
            TagBenchmark
        );
    }

    private static bool IsSane(BenchmarkStartRequest body) =>
        body.Players is > 0 and <= MaxPlayers
        && body.Furniture is >= 0 and <= MaxFurniture
        && body.DurationSeconds is > 0 and <= MaxDurationSeconds
        && body.RoomId >= 0
        && body.RampSeconds >= 0
        && body.WalkIntervalMs >= 0
        && body.ChatIntervalMs >= 0;
}
