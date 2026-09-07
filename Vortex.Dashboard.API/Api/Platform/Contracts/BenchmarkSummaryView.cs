namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// The run in five numbers, or absent when it took no samples.
/// </summary>
/// <param name="WorstRttMs">Over the whole run rather than the last sample: the tail is what a
/// player notices, and it does not show up in an average.</param>
public sealed record BenchmarkSummaryView(
    int PeakClients,
    double WorstRttMs,
    double MedianRttMs,
    long TotalPackets,
    long TotalBytes,
    long Failures
);
