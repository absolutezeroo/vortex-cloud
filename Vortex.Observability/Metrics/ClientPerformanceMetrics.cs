using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using Vortex.Observability.Diagnostics;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Metrics;

/// <summary>
/// Client-side rendering/performance telemetry (elapsed time, memory, frame rate, GC count) as OTel
/// instruments instead of one transactional-DB row per packet — every connected client sends this
/// periodically, so a per-event row was unbounded growth for data nobody queried per-row (only ever
/// summed as a total). Tagged only by OS/Browser (bounded dimensions) — never by player id.
/// </summary>
public sealed class ClientPerformanceMetrics : IPerformanceLogSink, IDisposable
{
    private readonly Meter _meter;
    private readonly Histogram<int> _elapsedTime;
    private readonly Histogram<int> _memoryUsage;
    private readonly Histogram<int> _averageFrameRate;
    private readonly Counter<long> _garbageCollections;
    private long _totalSamples;

    public ClientPerformanceMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(VortexTelemetry.Name, VortexTelemetry.Version);

        _elapsedTime = _meter.CreateHistogram<int>(
            "Vortex.client.performance.elapsed_time",
            unit: "ms",
            description: "Client-reported elapsed time per performance sample."
        );
        _memoryUsage = _meter.CreateHistogram<int>(
            "Vortex.client.performance.memory_usage",
            description: "Client-reported memory usage per performance sample."
        );
        _averageFrameRate = _meter.CreateHistogram<int>(
            "Vortex.client.performance.frame_rate",
            unit: "{frame}/s",
            description: "Client-reported average frame rate per performance sample."
        );
        _garbageCollections = _meter.CreateCounter<long>(
            "Vortex.client.performance.garbage_collections",
            unit: "{collection}",
            description: "Client-reported garbage collections since the previous sample."
        );
        _meter.CreateObservableCounter(
            "Vortex.client.performance.samples",
            () => Volatile.Read(ref _totalSamples),
            unit: "{sample}",
            description: "Total client performance samples received since process start."
        );
    }

    /// <summary>The running total, for surfacing on the dashboard overview (resets on restart).</summary>
    public long TotalSamples => Volatile.Read(ref _totalSamples);

    public void Record(in PerformanceLogEvent entry)
    {
        KeyValuePair<string, object?>[] tags = [new("os", entry.OS), new("browser", entry.Browser)];

        _elapsedTime.Record(entry.ElapsedTime, tags);
        _memoryUsage.Record(entry.MemoryUsage, tags);
        _averageFrameRate.Record(entry.AverageFrameRate, tags);
        _garbageCollections.Add(entry.GarbageCollections, tags);
        Interlocked.Increment(ref _totalSamples);
    }

    public void Dispose() => _meter.Dispose();
}
