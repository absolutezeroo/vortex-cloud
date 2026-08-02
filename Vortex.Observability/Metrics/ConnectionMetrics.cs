using System;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Observability.Configuration;
using Vortex.Observability.Diagnostics;
using Vortex.Primitives.Networking;

namespace Vortex.Observability.Metrics;

/// <summary>
/// Publishes the two connection counts the session gateway already tracks but never exposed:
/// established sessions and distinct signed-in players. Both are observable gauges — the value is
/// read from the gateway when a scrape happens, so nothing is pushed or cached and the two can never
/// drift from the live maps.
/// </summary>
/// <remarks>
/// Registered as a hosted service purely so the container builds it at startup: an observable gauge
/// that is never constructed is a gauge that never reports. It has no start-up work of its own.
/// The two counts differ on purpose — a session that has connected but not yet authenticated counts
/// as a session and not as an online player, so the gap between the curves is the login funnel.
/// </remarks>
public sealed class ConnectionMetrics : IHostedService, IDisposable
{
    private readonly Meter? _meter;

    public ConnectionMetrics(
        IMeterFactory meterFactory,
        ISessionGateway sessions,
        IOptions<ObservabilityConfig> options
    )
    {
        if (!options.Value.MetricsEnabled)
        {
            return;
        }

        _meter = meterFactory.Create(VortexTelemetry.Name, VortexTelemetry.Version);

        _meter.CreateObservableGauge(
            "Vortex.sessions.active",
            sessions.GetActiveSessionCount,
            unit: "{session}",
            description: "Connections currently held by the session gateway, authenticated or not."
        );

        _meter.CreateObservableGauge(
            "Vortex.players.online",
            () => sessions.GetOnlinePlayerIds().Count,
            unit: "{player}",
            description: "Distinct players currently mapped to a session."
        );
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose() => _meter?.Dispose();
}
