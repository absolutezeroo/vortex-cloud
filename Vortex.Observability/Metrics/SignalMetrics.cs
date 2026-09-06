using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Vortex.Observability.Diagnostics;
using Vortex.Primitives.Signals;

namespace Vortex.Observability.Metrics;

/// <summary>
/// Counters for the progress-signal layer, under the shared "Vortex" meter.
/// </summary>
/// <remarks>
/// <para>
/// A progression system that stops advancing raises no exception — content simply never completes,
/// and it surfaces weeks later as a player complaint. These four counters are what makes it visible
/// on a scrape instead.
/// </para>
/// <para>
/// The gate is counted per translator rather than per action because it runs before the translation:
/// at that point no signal exists and no action has been chosen, and one translator may declare
/// several. Published over gated, on a hotel running no campaign, should read 100% gated — that
/// ratio is the health of the hottest path in the hotel.
/// </para>
/// </remarks>
public sealed class SignalMetrics : ISignalMetrics, IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _gated;
    private readonly Counter<long> _published;
    private readonly Counter<long> _raised;
    private readonly Counter<long> _consumerFailures;

    public SignalMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        _meter = meterFactory.Create(VortexTelemetry.Name, VortexTelemetry.Version);

        _gated = _meter.CreateCounter<long>(
            "Vortex.signals.translations_gated",
            unit: "{event}",
            description: "Domain events the interest gate stopped before translating."
        );

        _published = _meter.CreateCounter<long>(
            "Vortex.signals.batches_published",
            unit: "{batch}",
            description: "Domain events that passed the gate and published a signal batch."
        );

        _raised = _meter.CreateCounter<long>(
            "Vortex.signals.raised",
            unit: "{signal}",
            description: "Individual progress signals published, by action."
        );

        _consumerFailures = _meter.CreateCounter<long>(
            "Vortex.signals.consumer_failures",
            unit: "{failure}",
            description: "Consumers that threw while handling a signal batch."
        );
    }

    public void TranslationGated(string translator) =>
        _gated.Add(1, new KeyValuePair<string, object?>("translator", translator));

    public void BatchPublished(string translator) =>
        _published.Add(1, new KeyValuePair<string, object?>("translator", translator));

    public void SignalRaised(string action) =>
        _raised.Add(1, new KeyValuePair<string, object?>("action", action));

    public void ConsumerFailed(string consumer) =>
        _consumerFailures.Add(1, new KeyValuePair<string, object?>("consumer", consumer));

    public void Dispose() => _meter.Dispose();
}
