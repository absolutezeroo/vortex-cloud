using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vortex.Observability.Runtime;
using Vortex.Primitives.Benchmark;

namespace Vortex.Benchmark;

/// <summary>
/// Writes a finished run to a file on disk.
/// </summary>
/// <remarks>
/// <para>
/// A number on a page cannot be handed to anyone. The point of a load test is the conversation that
/// follows it — "this got slower, look" — and that conversation needs an artefact: something to
/// attach, diff against last week's, or point a tool at. So every run leaves one behind, whether
/// anyone was watching the page or not.
/// </para>
/// <para>
/// It carries more than the client-side numbers. The round trips say <em>that</em> the hotel got
/// slower; the room tick breakdown says <em>which step</em> did, which is the difference between a
/// report and a lead. The process counters are there for the same reason: a run that spent its time
/// in garbage collection looks identical from the outside to one that spent it in the pathfinder.
/// </para>
/// </remarks>
internal sealed class BenchmarkReportWriter(
    IHostEnvironment environment,
    RoomPerformanceAggregator roomPerformance,
    ILogger<BenchmarkReportWriter> logger
)
{
    private static readonly JsonSerializerOptions Formatting = new() { WriteIndented = true };

    public string Directory => Path.Combine(environment.ContentRootPath, "logs", "benchmark");

    /// <summary>Returns the path written, or null when it could not be. A run whose report failed is
    /// still a run: the failure is logged and the result stands.</summary>
    public async Task<string?> WriteAsync(
        BenchmarkPlan plan,
        BenchmarkStatus status,
        ProcessCounters before,
        CancellationToken ct
    )
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);

            string stamp = DateTime.UtcNow.ToString(
                "yyyyMMdd-HHmmss",
                CultureInfo.InvariantCulture
            );
            string path = Path.Combine(Directory, $"run-{stamp}.json");

            RoomPerformanceSnapshot rooms = roomPerformance.GetSnapshot();
            ProcessCounters after = ProcessCounters.Capture();

            var report = new
            {
                schema = "vortex.benchmark/1",
                writtenAtUtc = DateTime.UtcNow,
                plan,
                outcome = new
                {
                    phase = status.Phase.ToString(),
                    status.StartedAtUtc,
                    status.EndedAtUtc,
                    status.RoomId,
                    status.BorrowedRoom,
                    status.PlacedFurniture,
                    status.Error,
                    status.Residue,
                },
                client = new
                {
                    peakClients = status.Samples.IsEmpty
                        ? 0
                        : status.Samples.Max(s => s.ConnectedClients),
                    worstRttMs = status.Samples.IsEmpty ? 0 : status.Samples.Max(s => s.RttP95Ms),
                    failures = status.Samples.IsEmpty ? 0 : status.Samples[^1].Failures,
                    samples = status.Samples,
                },
                // The half that says where the time went. Steps carry their share of the tick, so
                // the expensive one is the first line worth reading.
                rooms = new
                {
                    rooms.WindowSeconds,
                    tick = rooms.Tick,
                    steps = rooms.Steps.OrderByDescending(step => step.SumMs).ToList(),
                    directoryCalls = rooms.DirectoryCalls,
                },
                process = new
                {
                    before,
                    after,
                    gcDuringRun = new
                    {
                        gen0 = after.Gen0Collections - before.Gen0Collections,
                        gen1 = after.Gen1Collections - before.Gen1Collections,
                        gen2 = after.Gen2Collections - before.Gen2Collections,
                        allocatedMb = Math.Round(
                            (after.TotalAllocatedBytes - before.TotalAllocatedBytes)
                                / 1024d
                                / 1024d,
                            1
                        ),
                    },
                },
                environment = new
                {
                    Environment.ProcessorCount,
                    Environment.OSVersion.VersionString,
                    dotnet = Environment.Version.ToString(),
                    machine = Environment.MachineName,
                },
            };

            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, Formatting), ct)
                .ConfigureAwait(false);

            logger.LogInformation("Benchmark report written to {Path}", path);

            return path;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Benchmark report could not be written.");

            return null;
        }
    }
}

/// <summary>
/// What the process itself was doing, taken either side of a run.
/// </summary>
/// <remarks>
/// Cheap enough to take twice and worth having both times: a run that spent its budget collecting
/// garbage is indistinguishable, from the outside, from one that spent it doing work.
/// </remarks>
internal sealed record ProcessCounters
{
    public required long WorkingSetMb { get; init; }
    public required long ManagedHeapMb { get; init; }
    public required long TotalAllocatedBytes { get; init; }
    public required int Gen0Collections { get; init; }
    public required int Gen1Collections { get; init; }
    public required int Gen2Collections { get; init; }
    public required int ThreadCount { get; init; }

    public static ProcessCounters Capture()
    {
        using System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();

        return new ProcessCounters
        {
            WorkingSetMb = process.WorkingSet64 / 1024 / 1024,
            ManagedHeapMb = GC.GetTotalMemory(false) / 1024 / 1024,
            TotalAllocatedBytes = GC.GetTotalAllocatedBytes(false),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            ThreadCount = process.Threads.Count,
        };
    }
}
