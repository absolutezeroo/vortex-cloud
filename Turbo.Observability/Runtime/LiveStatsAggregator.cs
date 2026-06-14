using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Turbo.Observability.Configuration;

namespace Turbo.Observability.Runtime;

internal sealed class LiveStatsAggregator(IOptions<ObservabilityConfig> config) : ILiveStatsAggregator
{
    private readonly Lock _sync = new();
    private readonly TimeSpan _window = TimeSpan.FromSeconds(Math.Max(1, config.Value.LiveStatsWindowSeconds));
    private readonly int _topK = Math.Max(1, config.Value.LiveStatsTopK);
    private readonly Queue<DateTime> _receivedTimestamps = [];
    private readonly Queue<DateTime> _failedTimestamps = [];
    private readonly Queue<LatencySample> _latencySamples = [];
    private readonly Queue<AbuserSample> _abuserSamples = [];
    private readonly Dictionary<int, int> _abuserCounts = [];
    private readonly Queue<RoomSample> _roomSamples = [];
    private readonly Dictionary<int, int> _roomCounts = [];

    public void RecordPacketReceived(long? actorId = null, int? roomId = null)
    {
        var now = DateTime.UtcNow;

        lock (_sync)
        {
            Prune(now);
            _receivedTimestamps.Enqueue(now);

            if (actorId is > 0 && actorId <= int.MaxValue)
            {
                var actor = (int)actorId;
                _abuserSamples.Enqueue(new(now, actor));
                _abuserCounts[actor] = _abuserCounts.GetValueOrDefault(actor) + 1;
            }

            if (roomId is > 0)
            {
                _roomSamples.Enqueue(new(now, roomId.Value));
                _roomCounts[roomId.Value] = _roomCounts.GetValueOrDefault(roomId.Value) + 1;
            }
        }
    }

    public void RecordPacketCompleted(long? actorId = null, int? roomId = null, double elapsedMilliseconds = 0)
    {
        var now = DateTime.UtcNow;

        lock (_sync)
        {
            Prune(now);
            _latencySamples.Enqueue(new(now, elapsedMilliseconds));
        }
    }

    public void RecordPacketFailed(long? actorId = null, int? roomId = null)
    {
        var now = DateTime.UtcNow;

        lock (_sync)
        {
            Prune(now);
            _failedTimestamps.Enqueue(now);
        }
    }

    public Task<LiveStatsSnapshot> GetSnapshotAsync()
    {
        var now = DateTime.UtcNow;

        lock (_sync)
        {
            Prune(now);

            var latencies = _latencySamples
                .Select(sample => sample.DurationMs)
                .OrderBy(value => value)
                .ToArray();

            var topAbusers = _abuserCounts
                .OrderByDescending(item => item.Value)
                .ThenBy(item => item.Key)
                .Take(_topK)
                .Select(item => new LiveAbuserSnapshot(item.Key, ToRatePerMinute(item.Value)))
                .ToArray();

            return Task.FromResult(
                new LiveStatsSnapshot(
                    _receivedTimestamps.Count / _window.TotalSeconds,
                    _failedTimestamps.Count * 60 / _window.TotalSeconds,
                    Percentile(latencies, 0.50),
                    Percentile(latencies, 0.95),
                    topAbusers,
                    _roomCounts
                        .OrderByDescending(item => item.Value)
                        .ThenBy(item => item.Key)
                        .Take(_topK)
                        .Select(item => new LiveRoomSnapshot(item.Key, ToRatePerMinute(item.Value)))
                        .ToArray()
                )
            );
        }
    }

    private void Prune(DateTime now)
    {
        var cutoff = now - _window;

        while (_receivedTimestamps.Count > 0 && _receivedTimestamps.Peek() < cutoff)
            _receivedTimestamps.Dequeue();

        while (_failedTimestamps.Count > 0 && _failedTimestamps.Peek() < cutoff)
            _failedTimestamps.Dequeue();

        while (_latencySamples.Count > 0 && _latencySamples.Peek().Timestamp < cutoff)
            _latencySamples.Dequeue();

        while (_abuserSamples.Count > 0 && _abuserSamples.Peek().Timestamp < cutoff)
        {
            var sample = _abuserSamples.Dequeue();

            if (_abuserCounts.TryGetValue(sample.ActorId, out var total))
            {
                if (total <= 1)
                    _abuserCounts.Remove(sample.ActorId);
                else
                    _abuserCounts[sample.ActorId] = total - 1;
            }
        }

        while (_roomSamples.Count > 0 && _roomSamples.Peek().Timestamp < cutoff)
        {
            var sample = _roomSamples.Dequeue();

            if (_roomCounts.TryGetValue(sample.RoomId, out var total))
            {
                if (total <= 1)
                    _roomCounts.Remove(sample.RoomId);
                else
                    _roomCounts[sample.RoomId] = total - 1;
            }
        }
    }

    private static double Percentile(double[] values, double ratio)
    {
        if (values.Length == 0)
            return 0;

        var index = (int)Math.Ceiling((values.Length - 1) * ratio);
        return values[index];
    }

    private double ToRatePerMinute(int count) =>
        _window.TotalSeconds <= 0 ? 0 : count * 60d / _window.TotalSeconds;

    private sealed record LatencySample(DateTime Timestamp, double DurationMs);
    private sealed record AbuserSample(DateTime Timestamp, int ActorId);
    private sealed record RoomSample(DateTime Timestamp, int RoomId);
}

internal interface ILiveStatsAggregator
{
    void RecordPacketReceived(long? actorId = null, int? roomId = null);

    void RecordPacketCompleted(
        long? actorId = null,
        int? roomId = null,
        double elapsedMilliseconds = 0
    );

    void RecordPacketFailed(long? actorId = null, int? roomId = null);

    Task<LiveStatsSnapshot> GetSnapshotAsync();
}

internal sealed record LiveStatsSnapshot(
    double PacketsPerSecond,
    double ErrorsPerMinute,
    double LatencyP50Ms,
    double LatencyP95Ms,
    IReadOnlyList<LiveAbuserSnapshot> TopAbusers,
    IReadOnlyList<LiveRoomSnapshot> TopRooms
);

internal sealed record LiveAbuserSnapshot(int PlayerId, double PacketsPerMinute);
internal sealed record LiveRoomSnapshot(int RoomId, double PacketsPerMinute);
