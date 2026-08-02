using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Observability.Configuration;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Runtime;

/// <summary>
/// Reads the room instruments back out of the meter so the dashboard can show them.
/// </summary>
/// <remarks>
/// <para>
/// <c>System.Diagnostics.Metrics</c> is write-only by design — an instrument has no "current value"
/// to query. A <see cref="MeterListener"/> is the supported way to observe your own meter, and it is
/// what this uses: it subscribes to the three room histograms and keeps a rolling window of samples.
/// </para>
/// <para>
/// Listening rather than having <c>RoomGrain</c> report twice is the whole point. A second write path
/// would put another call in <c>RunTickStepAsync</c>, which runs twenty times a second per room and
/// was deliberately built to cost one boolean when metrics are off. It would also let the dashboard
/// and the Prometheus scrape disagree; here they are the same measurements, read once.
/// </para>
/// <para>
/// Percentiles are computed by sorting the retained window on read. That is fine at this cadence
/// (a scrape or a dashboard poll, not a packet) and it keeps the samples exact instead of bucketing
/// them twice.
/// </para>
/// </remarks>
public sealed class RoomPerformanceAggregator : IHostedService, IDisposable
{
    internal const string TickStepInstrument = "Vortex.room.tick.step.duration";
    internal const string TickInstrument = "Vortex.room.tick.duration";
    internal const string DirectoryInstrument = "Vortex.room.directory.call.duration";

    private const string StepTag = "step";
    private const string MethodTag = "method";

    /// <summary>
    /// Cap per series. Ten steps ticking at 20Hz per room fills a time-based window fast, and an
    /// unbounded queue here would be a slow leak in the one component meant to watch for leaks.
    /// </summary>
    private const int MaxSamplesPerSeries = 4096;

    private readonly MeterListener _listener = new();
    private readonly object _sync = new();
    private readonly TimeSpan _window;
    private readonly bool _enabled;

    private readonly Dictionary<string, Series> _steps = [];
    private readonly Dictionary<string, Series> _directory = [];
    private readonly Series _tick = new();

    public RoomPerformanceAggregator(IOptions<ObservabilityConfig> options)
    {
        _enabled = options.Value.MetricsEnabled;
        _window = TimeSpan.FromSeconds(Math.Max(1, options.Value.LiveStatsWindowSeconds));

        if (!_enabled)
        {
            return;
        }

        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (
                instrument.Meter.Name == VortexMeterNames.VORTEX
                && instrument.Name is TickStepInstrument or TickInstrument or DirectoryInstrument
            )
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<double>(
            (instrument, value, tags, _) => Record(instrument.Name, value, tags)
        );
    }

    /// <summary>
    /// Subscribing is deferred to host start rather than done in the constructor so the listener is
    /// not live while the container is still being built.
    /// </summary>
    public Task StartAsync(CancellationToken ct)
    {
        if (_enabled)
        {
            _listener.Start();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private void Record(
        string instrument,
        double value,
        ReadOnlySpan<KeyValuePair<string, object?>> tags
    )
    {
        DateTime now = DateTime.UtcNow;

        lock (_sync)
        {
            switch (instrument)
            {
                case TickInstrument:
                    _tick.Add(now, value, _window);
                    break;

                case TickStepInstrument:
                    AddTagged(_steps, StepTag, tags, now, value);
                    break;

                case DirectoryInstrument:
                    AddTagged(_directory, MethodTag, tags, now, value);
                    break;

                default:
                    break;
            }
        }
    }

    private void AddTagged(
        Dictionary<string, Series> target,
        string tagKey,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        DateTime now,
        double value
    )
    {
        string? key = null;

        foreach (KeyValuePair<string, object?> tag in tags)
        {
            if (tag.Key == tagKey)
            {
                key = tag.Value as string;
                break;
            }
        }

        if (key is null)
        {
            return;
        }

        if (!target.TryGetValue(key, out Series? series))
        {
            series = new Series();
            target[key] = series;
        }

        series.Add(now, value, _window);
    }

    /// <summary>Current view of the rolling window, safe to serialise straight to the dashboard.</summary>
    public RoomPerformanceSnapshot GetSnapshot()
    {
        DateTime now = DateTime.UtcNow;

        lock (_sync)
        {
            _tick.Prune(now, _window);

            foreach (Series series in _steps.Values)
            {
                series.Prune(now, _window);
            }

            foreach (Series series in _directory.Values)
            {
                series.Prune(now, _window);
            }

            // Steps are shown as a share of the tick they belong to, which is what makes the table
            // actionable: "pets is 73% of the tick" is the sentence an operator acts on, not "pets
            // took 0.67ms".
            double stepTotal = _steps.Values.Sum(s => s.Sum);

            return new RoomPerformanceSnapshot(
                WindowSeconds: (int)_window.TotalSeconds,
                Tick: _tick.ToStats("tick", 0),
                Steps:
                [
                    .. _steps
                        .Select(kv => kv.Value.ToStats(kv.Key, stepTotal))
                        .OrderByDescending(s => s.SumMs),
                ],
                DirectoryCalls:
                [
                    .. _directory
                        .Select(kv => kv.Value.ToStats(kv.Key, 0))
                        .OrderByDescending(s => s.SumMs),
                ]
            );
        }
    }

    public void Dispose() => _listener.Dispose();

    /// <summary>One instrument/tag combination's retained samples.</summary>
    private sealed class Series
    {
        private readonly Queue<(DateTime At, double Value)> _samples = [];

        public double Sum { get; private set; }

        public void Add(DateTime now, double value, TimeSpan window)
        {
            Prune(now, window);
            _samples.Enqueue((now, value));
            Sum += value;

            while (_samples.Count > MaxSamplesPerSeries)
            {
                Sum -= _samples.Dequeue().Value;
            }
        }

        public void Prune(DateTime now, TimeSpan window)
        {
            DateTime cutoff = now - window;

            while (_samples.Count > 0 && _samples.Peek().At < cutoff)
            {
                Sum -= _samples.Dequeue().Value;
            }
        }

        public RoomPerformanceSeriesStats ToStats(string name, double totalForShare)
        {
            if (_samples.Count == 0)
            {
                return new RoomPerformanceSeriesStats(name, 0, 0, 0, 0, 0, 0);
            }

            double[] ordered = [.. _samples.Select(s => s.Value).Order()];

            return new RoomPerformanceSeriesStats(
                Name: name,
                Count: ordered.Length,
                P50Ms: Percentile(ordered, 0.50),
                P95Ms: Percentile(ordered, 0.95),
                P99Ms: Percentile(ordered, 0.99),
                SumMs: Sum,
                ShareOfTickPercent: totalForShare > 0 ? Sum / totalForShare * 100 : 0
            );
        }

        private static double Percentile(double[] ordered, double percentile)
        {
            int index = (int)Math.Ceiling(percentile * ordered.Length) - 1;

            return ordered[Math.Clamp(index, 0, ordered.Length - 1)];
        }
    }
}

/// <param name="ShareOfTickPercent">
/// This step's share of all step time in the window. Zero for series where a share is meaningless
/// (the tick total itself, and the directory calls, which are not part of a tick).
/// </param>
public sealed record RoomPerformanceSeriesStats(
    string Name,
    int Count,
    double P50Ms,
    double P95Ms,
    double P99Ms,
    double SumMs,
    double ShareOfTickPercent
);

public sealed record RoomPerformanceSnapshot(
    int WindowSeconds,
    RoomPerformanceSeriesStats Tick,
    IReadOnlyList<RoomPerformanceSeriesStats> Steps,
    IReadOnlyList<RoomPerformanceSeriesStats> DirectoryCalls
);
