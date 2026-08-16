using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
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
internal sealed partial class BenchmarkReportWriter(
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
                // The judgement, made once and stored with the numbers so the file, the page and
                // the history list can never disagree about whether a run was good.
                verdict = BenchmarkVerdict.Evaluate(status.Samples, rooms.Tick.P99Ms),
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

    /// <summary>
    /// One report, read back whole.
    /// </summary>
    /// <remarks>
    /// The name is checked against the pattern this writer produces rather than trusted: it arrives
    /// from a URL, and a name is all it takes to read any file on the machine if nobody looks.
    /// </remarks>
    public async Task<string?> ReadAsync(string fileName, CancellationToken ct)
    {
        if (!ReportName().IsMatch(fileName))
        {
            return null;
        }

        string path = Path.Combine(Directory, fileName);

        return File.Exists(path)
            ? await File.ReadAllTextAsync(path, ct).ConfigureAwait(false)
            : null;
    }

    [GeneratedRegex(@"^run-\d{8}-\d{6}\.json$")]
    private static partial Regex ReportName();

    /// <summary>
    /// The runs already on disk, newest first, with just enough of each to tell them apart.
    /// </summary>
    /// <remarks>
    /// Read from the files rather than kept in memory: the service holds one run at a time by
    /// design, and the whole point of writing them down was that they outlive the process. A restart
    /// loses nothing.
    /// </remarks>
    public ImmutableArray<BenchmarkRunSummary> List(int limit)
    {
        try
        {
            if (!System.IO.Directory.Exists(Directory))
            {
                return [];
            }

            return
            [
                .. new DirectoryInfo(Directory)
                    .GetFiles("run-*.json")
                    .OrderByDescending(file => file.CreationTimeUtc)
                    .Take(limit)
                    .Select(Describe)
                    .OfType<BenchmarkRunSummary>(),
            ];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Benchmark reports could not be listed.");

            return [];
        }
    }

    /// <summary>
    /// Pulls the headline out of one report. A file written by an older schema, or half-written by a
    /// process that died mid-flush, is skipped rather than allowed to take the whole list down.
    /// </summary>
    private static BenchmarkRunSummary? Describe(FileInfo file)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(file.FullName));
            JsonElement root = document.RootElement;

            JsonElement plan = root.GetProperty("plan");
            JsonElement outcome = root.GetProperty("outcome");
            JsonElement client = root.GetProperty("client");

            return new BenchmarkRunSummary
            {
                FileName = file.Name,
                Path = file.FullName,
                SizeBytes = file.Length,
                WrittenAtUtc = root.GetProperty("writtenAtUtc").GetDateTime(),
                Players = plan.GetProperty("Players").GetInt32(),
                Furniture = plan.GetProperty("Furniture").GetInt32(),
                DurationSeconds = plan.GetProperty("DurationSeconds").GetInt32(),
                Label = plan.GetProperty("Label").GetString() ?? string.Empty,
                Phase = outcome.GetProperty("phase").GetString() ?? string.Empty,
                RoomId = outcome.GetProperty("RoomId").GetInt32(),
                BorrowedRoom = outcome.GetProperty("BorrowedRoom").GetBoolean(),
                PeakClients = client.GetProperty("peakClients").GetInt32(),
                WorstRttMs = client.GetProperty("worstRttMs").GetDouble(),
                Failures = client.GetProperty("failures").GetInt64(),
                Grade = root.TryGetProperty("verdict", out JsonElement verdict)
                    ? verdict.GetProperty("Grade").GetString() ?? string.Empty
                    : string.Empty,
                Headline = root.TryGetProperty("verdict", out JsonElement head)
                    ? head.GetProperty("Headline").GetString() ?? string.Empty
                    : string.Empty,
            };
        }
        catch (Exception)
        {
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
