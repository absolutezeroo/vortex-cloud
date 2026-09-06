using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals;

/// <summary>
/// Runs one translator on the event pipeline: gate, translate, publish.
/// </summary>
/// <remarks>
/// <para>
/// A singleton, built once by <see cref="SignalTranslatorFeatureProcessor"/> and registered as the
/// handler for its event. That is a deliberate difference from every other handler in the hotel,
/// which the pipeline instantiates and disposes on each invocation: a translator has no state and
/// no dependency, so paying an activation per walked tile would be pure waste.
/// </para>
/// <para>
/// It also owns the two rules that would otherwise be copied into twenty-one translators and
/// forgotten in one of them: the interest gate, and republishing the target as a fact.
/// </para>
/// </remarks>
public sealed class SignalTranslatorHost<TEvent>(
    ISignalTranslator<TEvent> translator,
    ImmutableArray<string> actions,
    IEventPublisher publisher,
    ISignalInterest interest,
    ISignalMetrics metrics,
    string translatorName
) : IEventHandler<TEvent>
    where TEvent : IEvent
{
    public async ValueTask HandleAsync(TEvent e, EventContext ctx, CancellationToken ct)
    {
        // First, before any allocation. On a hotel running no campaign this is the whole cost of
        // the subsystem, and this event may be a tile being walked onto by every avatar in every
        // room -- the room tick publishes it detached precisely because it expects to be stopped
        // here.
        if (!CaresAboutAny())
        {
            metrics.TranslationGated(translatorName);

            return;
        }

        ImmutableArray<ProgressSignal> signals = translator.Translate(e);

        if (signals.IsDefaultOrEmpty)
        {
            return;
        }

        ImmutableArray<ProgressSignal> published = WithTargetFacts(signals);

        foreach (ProgressSignal signal in published)
        {
            metrics.SignalRaised(signal.Action);
        }

        metrics.BatchPublished(translatorName);

        await publisher
            .PublishAsync(new ProgressSignalsRaised(translator.DeliveryIdOf(e), published), ct)
            .ConfigureAwait(false);
    }

    private bool CaresAboutAny()
    {
        foreach (string action in actions)
        {
            if (interest.AnyConsumerCares(action))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Republishes <see cref="ProgressSignal.Target"/> as the <c>target</c> fact, which is what
    /// <c>RewardTrackSignal.SendAsync</c> did for every signal it sent.
    /// </summary>
    /// <remarks>
    /// Twelve actions declare <c>target</c> among their facts and content filters on it, so leaving
    /// it out would silently stop those filters matching — a build-clean, test-clean regression, and
    /// exactly the one an early draft of this design shipped in its own example. Done here rather
    /// than in each translator so it cannot be the one thing somebody forgets.
    /// </remarks>
    private static ImmutableArray<ProgressSignal> WithTargetFacts(
        ImmutableArray<ProgressSignal> signals
    )
    {
        ImmutableArray<ProgressSignal>.Builder builder =
            ImmutableArray.CreateBuilder<ProgressSignal>(signals.Length);

        foreach (ProgressSignal signal in signals)
        {
            builder.Add(
                string.IsNullOrEmpty(signal.Target)
                    ? signal
                    : signal with
                    {
                        Facts = signal.Facts.Insert(
                            0,
                            new SignalFact(Facts.TargetKey, signal.Target)
                        ),
                    }
            );
        }

        return builder.MoveToImmutable();
    }
}
