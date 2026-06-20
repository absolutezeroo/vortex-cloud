using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using Turbo.Observability.Configuration;
using Turbo.Observability.Diagnostics;
using Turbo.Observability.Runtime;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.Metrics;

/// <summary>
/// <c>System.Diagnostics.Metrics</c>-based implementation of <see cref="ITurboMetrics"/>. Instruments
/// live under the shared "Turbo" meter and are tagged only by bounded dimensions (the operation
/// name) to keep cardinality safe for Prometheus/OpenTelemetry exporters. Never tag by user id or
/// room id — high-cardinality breakdowns belong to in-memory aggregators, not metric tags.
/// </summary>
public sealed class TurboMetrics : ITurboMetrics, IDisposable
{
    private readonly bool _enabled;
    private readonly Meter _meter;
    private readonly Counter<long> _packetReceived;
    private readonly Histogram<double> _packetDuration;
    private readonly Counter<long> _packetFailed;
    private readonly ILiveStatsAggregator _liveStats;

    public TurboMetrics(
        IMeterFactory meterFactory,
        ILiveStatsAggregator liveStats,
        IOptions<ObservabilityConfig> options
    )
    {
        _enabled = options.Value.MetricsEnabled;
        _meter = meterFactory.Create(TurboTelemetry.Name, TurboTelemetry.Version);
        _liveStats = liveStats;

        _packetReceived = _meter.CreateCounter<long>(
            "turbo.packet.received",
            unit: "{packet}",
            description: "Inbound packets accepted for dispatch."
        );
        _packetDuration = _meter.CreateHistogram<double>(
            "turbo.packet.duration",
            unit: "ms",
            description: "End-to-end handler dispatch time per packet."
        );
        _packetFailed = _meter.CreateCounter<long>(
            "turbo.packet.failed",
            unit: "{packet}",
            description: "Packets whose dispatch threw an exception."
        );
    }

    public void PacketReceived(string operation, long? actorId = null, int? roomId = null)
    {
        if (_enabled)
        {
            _packetReceived.Add(1, Tag(operation));
        }

        _liveStats.RecordPacketReceived(operation, actorId, roomId);
    }

    public void PacketCompleted(
        string operation,
        double elapsedMilliseconds,
        long? actorId = null,
        int? roomId = null
    )
    {
        if (_enabled)
        {
            _packetDuration.Record(elapsedMilliseconds, Tag(operation));
        }

        _liveStats.RecordPacketCompleted(actorId, roomId, elapsedMilliseconds);
    }

    public void PacketFailed(string operation, long? actorId = null, int? roomId = null)
    {
        if (_enabled)
        {
            _packetFailed.Add(1, Tag(operation));
        }

        _liveStats.RecordPacketFailed(operation, actorId, roomId);
    }

    private static KeyValuePair<string, object?> Tag(string operation) =>
        new("operation", operation);

    public void Dispose() => _meter.Dispose();
}
