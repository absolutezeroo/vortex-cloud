namespace Turbo.Primitives.Observability;

/// <summary>
/// Client-side rendering/performance telemetry payload for offline analysis and incident correlation.
/// Implementations should remain non-blocking and durable via background pipelines.
/// </summary>
public readonly record struct PerformanceLogEvent
{
    public required int ElapsedTime { get; init; }

    public required string UserAgent { get; init; }

    public required string FlashVersion { get; init; }

    public required string OS { get; init; }

    public required string Browser { get; init; }

    public required bool IsDebugger { get; init; }

    public required int MemoryUsage { get; init; }

    public required int GarbageCollections { get; init; }

    public required int AverageFrameRate { get; init; }

    public required string IPAddress { get; init; }
}
