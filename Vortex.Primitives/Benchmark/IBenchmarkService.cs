using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Benchmark;

/// <summary>
/// Drives a load run against this hotel and reports what it cost.
/// </summary>
/// <remarks>
/// <para>
/// One run at a time, deliberately. Two overlapping runs would each be measuring the other, and the
/// numbers would describe nothing that will ever happen in production.
/// </para>
/// <para>
/// This is a loaded gun: it opens real connections, writes real rows and competes with real players
/// for the same room ticks. It refuses to start unless <c>benchmark.enabled</c> says so, and it
/// removes everything it created before it reports — including after a failure, which is the run
/// most likely to leave a mess.
/// </para>
/// </remarks>
public interface IBenchmarkService
{
    Task<BenchmarkStartResult> StartAsync(BenchmarkPlan plan, CancellationToken ct);

    /// <summary>Ends the run early. The teardown still happens in full.</summary>
    Task StopAsync(CancellationToken ct);

    BenchmarkStatus GetStatus();

    /// <summary>
    /// Re-reads <c>benchmark.enabled</c>. Called before a status is reported so the page shows the
    /// switch as it is now, not as it was when somebody last tried to start a run.
    /// </summary>
    Task<bool> ReadEnabledAsync();

    /// <summary>
    /// The runs already written to disk, newest first. Read from the files themselves, so a restart
    /// does not lose the history — which is the whole reason they are files.
    /// </summary>
    ImmutableArray<BenchmarkRunSummary> ListRuns(int limit);

    /// <summary>One past report, read back whole, or null if there is no such run.</summary>
    Task<string?> ReadRunAsync(string fileName, CancellationToken ct);
}
