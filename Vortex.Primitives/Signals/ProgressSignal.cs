using System.Collections.Immutable;
using Vortex.Primitives.Events;

namespace Vortex.Primitives.Signals;

/// <summary>
/// What a player just did, said once for everyone.
/// </summary>
/// <remarks>
/// <para>
/// The canonical form every progression system reads. Before it, the same room entry was translated
/// three times — once for reward tracks, once for daily tasks, once for achievements — each with its
/// own vocabulary, so enriching an event only ever helped the system whose handler was edited.
/// </para>
/// <para>
/// Values are strings on purpose. That is already what the cross-step capture serialises onto the
/// player's row, and a typed <c>object</c> would force every consumer to know each fact's type. The
/// type lives in <see cref="FactKey"/>, where the editor needs it; the engine only ever compares.
/// </para>
/// </remarks>
/// <param name="Amount">
/// 1 for an act that happened, N for "spent N credits", the level reached for a pet level-up.
/// </param>
/// <param name="Target">
/// What the signal is mainly about. A task's <c>Parameter</c> is matched against it, and it is the
/// dedupe key for distinct-mode tasks — "visit 20 different rooms" counts 20 distinct targets. It is
/// also republished as the <c>target</c> fact, but by the host rather than by each translator; see
/// <see cref="ISignalTranslator{TEvent}"/>.
/// </param>
public sealed record ProgressSignal(
    long PlayerId,
    string Action,
    int Amount,
    string? Target,
    ImmutableArray<SignalFact> Facts
);

/// <summary>One named fact about what happened.</summary>
public readonly record struct SignalFact(string Key, string Value);

/// <summary>
/// What one domain event produced: a batch, not a signal.
/// </summary>
/// <remarks>
/// <para>
/// A source event routinely produces more than one signal — a catalogue purchase raises both
/// <c>buy_from_catalogue</c> and <c>spend_credits</c>, a completed trade raises one per participant,
/// equipping badges raises one per badge. The batch is what carries the delivery identity, and that
/// is not a cosmetic choice: <c>CommerceReplayGuard</c> writes a receipt row unique on
/// (operation, consumer), so a consumer guarding once per <em>signal</em> would have the second
/// signal of a purchase rejected by the receipt the first one wrote, and the credits spent would
/// never count. One batch means one guard per source event, exactly as the handlers did.
/// </para>
/// <para>
/// It also means the overhead of this design is one envelope per translated event rather than one
/// per action.
/// </para>
/// </remarks>
/// <param name="DeliveryId">
/// The source event's operation id, or empty when the event is not replayable — which is every event
/// except the catalogue purchase. An empty id makes the replay guard a no-op.
/// </param>
public sealed record ProgressSignalsRaised(
    string DeliveryId,
    ImmutableArray<ProgressSignal> Signals
) : IEvent;
