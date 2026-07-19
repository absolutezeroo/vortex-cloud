using System;
using System.Diagnostics.Metrics;
using System.Threading;
using Vortex.Observability.Diagnostics;

namespace Vortex.Observability.Metrics;

/// <summary>
/// Exposes a live gauge of active Habbo Club subscribers under the shared "Turbo" meter. The value
/// is pulled from a cached count refreshed off the hot path (see the refresh service), so scrapes
/// never touch the database directly.
/// </summary>
public sealed class ClubMetrics : IDisposable
{
    private readonly Meter _meter;
    private int _activeSubscribers;

    public ClubMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(TurboTelemetry.Name, TurboTelemetry.Version);

        _meter.CreateObservableGauge(
            "Vortex.club.active_subscribers",
            () => Volatile.Read(ref _activeSubscribers),
            unit: "{subscriber}",
            description: "Players with an active Habbo Club / VIP membership."
        );
    }

    public void SetActiveSubscribers(int count) => Volatile.Write(ref _activeSubscribers, count);

    /// <summary>The last refreshed active-subscriber count, for surfacing on the dashboard overview.</summary>
    public int ActiveSubscribers => Volatile.Read(ref _activeSubscribers);

    public void Dispose() => _meter.Dispose();
}
