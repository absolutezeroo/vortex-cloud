using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Events.Registry;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Snapshots;
using Vortex.Primitives.Signals;

namespace Vortex.RewardTracks.Events;

/// <summary>
/// The whole of the bridge from gameplay to reward tracks: one handler, on one event.
/// </summary>
/// <remarks>
/// <para>
/// It replaces twenty-two handlers that each translated a domain event into an action code. The
/// translation moved to <c>Vortex.Signals</c>, where it is a pure function and can be tested; what
/// is left here is the part that is genuinely about reward tracks — is this action in the content,
/// has this delivery been seen, and which grain gets told.
/// </para>
/// <para>
/// This consumer knows nothing about which events exist, and nothing about what a task is. The
/// grain still does the fine sort through <c>TasksFor</c>, exactly as before.
/// </para>
/// </remarks>
public sealed class RewardTrackSignalConsumer(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog,
    ICommerceJournal journal
) : IEventHandler<ProgressSignalsRaised>
{
    /// <summary>The replay-guard key. Per consumer, and it must stay that way.</summary>
    private const string CONSUMER = "reward-track";

    public async ValueTask HandleAsync(
        ProgressSignalsRaised e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        // Filtered first, and materialised. Filtering first keeps the replay receipt from being
        // spent on a batch none of whose signals concern this consumer -- after which a later
        // republication, once an operator has published content that does care, would be rejected.
        // Materialising closes the other window: the enumeration is read again after the guard's
        // await, and IsActionInteresting reads an index that a content write swaps atomically, so a
        // lazy sequence could be guarded for one set of signals and applied for another.
        ImmutableArray<ProgressSignal> mine = Interesting(e.Signals);

        if (mine.IsEmpty)
        {
            return;
        }

        // Once per source event, which is what the catalogue-purchase handler did before: it guarded
        // once and then raised both of its actions. Guarding per signal would have the second one
        // rejected by the receipt the first one wrote.
        if (
            !await CommerceReplayGuard
                .FirstDeliveryAsync(journal, e.DeliveryId, CONSUMER, ct)
                .ConfigureAwait(false)
        )
        {
            return;
        }

        foreach (ProgressSignal signal in mine)
        {
            await grainFactory
                .GetPlayerRewardTrackGrain(signal.PlayerId)
                .ProgressAsync(
                    signal.Action,
                    signal.Amount,
                    signal.Target,
                    ToSnapshots(signal.Facts),
                    ct
                )
                .ConfigureAwait(false);
        }
    }

    private ImmutableArray<ProgressSignal> Interesting(ImmutableArray<ProgressSignal> signals)
    {
        if (signals.IsDefaultOrEmpty)
        {
            return [];
        }

        ImmutableArray<ProgressSignal>.Builder? builder = null;

        foreach (ProgressSignal signal in signals)
        {
            if (signal.PlayerId <= 0 || !catalog.IsActionInteresting(signal.Action))
            {
                continue;
            }

            builder ??= ImmutableArray.CreateBuilder<ProgressSignal>(signals.Length);
            builder.Add(signal);
        }

        return builder?.ToImmutable() ?? [];
    }

    private static ImmutableArray<RewardTrackFactSnapshot> ToSnapshots(
        ImmutableArray<SignalFact> facts
    )
    {
        if (facts.IsDefaultOrEmpty)
        {
            return [];
        }

        ImmutableArray<RewardTrackFactSnapshot>.Builder builder =
            ImmutableArray.CreateBuilder<RewardTrackFactSnapshot>(facts.Length);

        foreach (SignalFact fact in facts)
        {
            builder.Add(new RewardTrackFactSnapshot(fact.Key, fact.Value));
        }

        return builder.MoveToImmutable();
    }
}

/// <summary>
/// Which actions reward-track content is currently defined on.
/// </summary>
/// <remarks>
/// A dedicated singleton rather than the consumer itself, because handlers are not services: the
/// feature processor registers an activator and the instance is built and dropped per invocation, so
/// there would be nothing for the gate to hold. Reads the catalogue's live index, so publishing a
/// campaign takes effect on the next event with no cache to invalidate.
/// </remarks>
public sealed class RewardTrackSignalInterest(IRewardTrackCatalog catalog) : ISignalInterestSource
{
    public ImmutableHashSet<string> Actions => catalog.Actions;
}
