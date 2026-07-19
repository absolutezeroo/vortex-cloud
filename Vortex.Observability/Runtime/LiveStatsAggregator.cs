using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Vortex.Observability.Configuration;

namespace Vortex.Observability.Runtime;

public sealed class LiveStatsAggregator(IOptions<ObservabilityConfig> config) : ILiveStatsAggregator
{
    private readonly Dictionary<int, int> _abuserCounts = [];
    private readonly Queue<AbuserSample> _abuserSamples = [];
    private readonly Dictionary<string, int> _failedOperationCounts = [];
    private readonly Queue<OperationSample> _failedOperationSamples = [];
    private readonly Queue<DateTime> _failedTimestamps = [];
    private readonly Queue<LatencySample> _latencySamples = [];
    private readonly Dictionary<string, int> _operationCounts = [];
    private readonly Queue<OperationSample> _operationSamples = [];
    private readonly Queue<DateTime> _receivedTimestamps = [];
    private readonly Dictionary<int, int> _roomCounts = [];
    private readonly Queue<RoomSample> _roomSamples = [];
    private readonly object _sync = new();
    private readonly int _topK = Math.Max(1, config.Value.LiveStatsTopK);

    private readonly TimeSpan _window = TimeSpan.FromSeconds(
        Math.Max(1, config.Value.LiveStatsWindowSeconds)
    );

    public void RecordPacketReceived(string operation, long? actorId = null, int? roomId = null)
    {
        DateTime now = DateTime.UtcNow;

        lock (_sync)
        {
            Prune(now);
            _receivedTimestamps.Enqueue(now);
            _operationSamples.Enqueue(new OperationSample(now, operation));
            _operationCounts[operation] = _operationCounts.GetValueOrDefault(operation) + 1;

            if (actorId is > 0 && actorId <= int.MaxValue)
            {
                int actor = (int)actorId;
                _abuserSamples.Enqueue(new AbuserSample(now, actor));
                _abuserCounts[actor] = _abuserCounts.GetValueOrDefault(actor) + 1;
            }

            if (roomId is > 0)
            {
                _roomSamples.Enqueue(new RoomSample(now, roomId.Value));
                _roomCounts[roomId.Value] = _roomCounts.GetValueOrDefault(roomId.Value) + 1;
            }
        }
    }

    public void RecordPacketCompleted(
        long? actorId = null,
        int? roomId = null,
        double elapsedMilliseconds = 0
    )
    {
        DateTime now = DateTime.UtcNow;

        lock (_sync)
        {
            Prune(now);
            _latencySamples.Enqueue(new LatencySample(now, elapsedMilliseconds));
        }
    }

    public void RecordPacketFailed(string operation, long? actorId = null, int? roomId = null)
    {
        DateTime now = DateTime.UtcNow;

        lock (_sync)
        {
            Prune(now);
            _failedTimestamps.Enqueue(now);
            _failedOperationSamples.Enqueue(new OperationSample(now, operation));
            _failedOperationCounts[operation] =
                _failedOperationCounts.GetValueOrDefault(operation) + 1;
        }
    }

    public Task<LiveStatsSnapshot> GetSnapshotAsync()
    {
        DateTime now = DateTime.UtcNow;

        lock (_sync)
        {
            Prune(now);

            double[] latencies = _latencySamples
                .Select(sample => sample.DurationMs)
                .OrderBy(value => value)
                .ToArray();

            LiveAbuserSnapshot[] topAbusers = _abuserCounts
                .OrderByDescending(item => item.Value)
                .ThenBy(item => item.Key)
                .Take(_topK)
                .Select(item => new LiveAbuserSnapshot(item.Key, ToRatePerMinute(item.Value)))
                .ToArray();

            LivePacketOperationSnapshot[] topOperations = _operationCounts
                .OrderByDescending(item => item.Value)
                .ThenBy(item => item.Key)
                .Take(_topK)
                .Select(item => new LivePacketOperationSnapshot(
                    item.Key,
                    ToRatePerMinute(item.Value)
                ))
                .ToArray();

            LivePacketOperationSnapshot[] topFailedOperations = _failedOperationCounts
                .OrderByDescending(item => item.Value)
                .ThenBy(item => item.Key)
                .Take(_topK)
                .Select(item => new LivePacketOperationSnapshot(
                    item.Key,
                    ToRatePerMinute(item.Value)
                ))
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
                        .ToArray(),
                    topOperations,
                    topFailedOperations
                )
            );
        }
    }

    private void Prune(DateTime now)
    {
        DateTime cutoff = now - _window;

        while (_receivedTimestamps.Count > 0 && _receivedTimestamps.Peek() < cutoff)
        {
            _receivedTimestamps.Dequeue();
        }

        while (_failedTimestamps.Count > 0 && _failedTimestamps.Peek() < cutoff)
        {
            _failedTimestamps.Dequeue();
        }

        while (_latencySamples.Count > 0 && _latencySamples.Peek().Timestamp < cutoff)
        {
            _latencySamples.Dequeue();
        }

        while (_operationSamples.Count > 0 && _operationSamples.Peek().Timestamp < cutoff)
        {
            OperationSample sample = _operationSamples.Dequeue();

            if (_operationCounts.TryGetValue(sample.Operation, out int total))
            {
                if (total <= 1)
                {
                    _operationCounts.Remove(sample.Operation);
                }
                else
                {
                    _operationCounts[sample.Operation] = total - 1;
                }
            }
        }

        while (
            _failedOperationSamples.Count > 0 && _failedOperationSamples.Peek().Timestamp < cutoff
        )
        {
            OperationSample sample = _failedOperationSamples.Dequeue();

            if (_failedOperationCounts.TryGetValue(sample.Operation, out int total))
            {
                if (total <= 1)
                {
                    _failedOperationCounts.Remove(sample.Operation);
                }
                else
                {
                    _failedOperationCounts[sample.Operation] = total - 1;
                }
            }
        }

        while (_abuserSamples.Count > 0 && _abuserSamples.Peek().Timestamp < cutoff)
        {
            AbuserSample sample = _abuserSamples.Dequeue();

            if (_abuserCounts.TryGetValue(sample.ActorId, out int total))
            {
                if (total <= 1)
                {
                    _abuserCounts.Remove(sample.ActorId);
                }
                else
                {
                    _abuserCounts[sample.ActorId] = total - 1;
                }
            }
        }

        while (_roomSamples.Count > 0 && _roomSamples.Peek().Timestamp < cutoff)
        {
            RoomSample sample = _roomSamples.Dequeue();

            if (_roomCounts.TryGetValue(sample.RoomId, out int total))
            {
                if (total <= 1)
                {
                    _roomCounts.Remove(sample.RoomId);
                }
                else
                {
                    _roomCounts[sample.RoomId] = total - 1;
                }
            }
        }
    }

    private static double Percentile(double[] values, double ratio)
    {
        if (values.Length == 0)
        {
            return 0;
        }

        int index = (int)Math.Ceiling((values.Length - 1) * ratio);
        return values[index];
    }

    private double ToRatePerMinute(int count)
    {
        return _window.TotalSeconds <= 0 ? 0 : count * 60d / _window.TotalSeconds;
    }

    private sealed record LatencySample(DateTime Timestamp, double DurationMs);

    private sealed record AbuserSample(DateTime Timestamp, int ActorId);

    private sealed record RoomSample(DateTime Timestamp, int RoomId);

    private sealed record OperationSample(DateTime Timestamp, string Operation);
}

public interface ILiveStatsAggregator
{
    void RecordPacketReceived(string operation, long? actorId = null, int? roomId = null);

    void RecordPacketCompleted(
        long? actorId = null,
        int? roomId = null,
        double elapsedMilliseconds = 0
    );

    void RecordPacketFailed(string operation, long? actorId = null, int? roomId = null);

    Task<LiveStatsSnapshot> GetSnapshotAsync();
}

public sealed record LiveStatsSnapshot(
    double PacketsPerSecond,
    double ErrorsPerMinute,
    double LatencyP50Ms,
    double LatencyP95Ms,
    IReadOnlyList<LiveAbuserSnapshot> TopAbusers,
    IReadOnlyList<LiveRoomSnapshot> TopRooms,
    IReadOnlyList<LivePacketOperationSnapshot> TopOperations,
    IReadOnlyList<LivePacketOperationSnapshot> TopFailedOperations
);

public sealed record LiveAbuserSnapshot(int PlayerId, double PacketsPerMinute);

public sealed record LiveRoomSnapshot(int RoomId, double PacketsPerMinute);

public sealed record LivePacketOperationSnapshot(string Operation, double PacketsPerMinute);
