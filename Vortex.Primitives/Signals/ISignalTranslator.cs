using System.Collections.Immutable;
using Vortex.Primitives.Events;

namespace Vortex.Primitives.Signals;

/// <summary>
/// Turns one domain event into the signals it means. A pure function, and the unit of extension.
/// </summary>
/// <remarks>
/// <para>
/// A translator has no constructor and no dependency, which is the point: the previous shape welded
/// the translation to an Orleans handler needing an <c>IGrainFactory</c>, so it could not be driven
/// from a unit test without standing up half a silo — and the test that was supposed to stop the
/// vocabulary drifting was therefore never written, and it drifted on nine actions.
/// </para>
/// <para>
/// <see cref="Shapes"/> is a static abstract member, so a translator that forgets to describe itself
/// does not compile. It is the single source of truth: the dashboard offers what it declares and the
/// content validator refuses what it does not, instead of both reading a map kept in step by hand.
/// </para>
/// <para>
/// A translator does not add the <c>target</c> fact and does not check whether anyone is listening.
/// The host does both, once, for all of them — those are exactly the two rules that get forgotten
/// when they are copied into twenty-one files.
/// </para>
/// </remarks>
public interface ISignalTranslator<TEvent>
    where TEvent : IEvent
{
    /// <summary>
    /// One shape per action this translator can produce, listing the union of facts that action's
    /// signals may carry.
    /// </summary>
    static abstract ImmutableArray<SignalShape> Shapes { get; }

    /// <summary>
    /// Zero, one or many signals.
    /// </summary>
    /// <remarks>
    /// Empty is a normal answer, and replaces the guard clauses the handlers opened with: a whisper,
    /// an unknown gesture, a purchase that cost nothing. Many is normal too — a purchase raises two
    /// actions, a trade one signal per participant, equipping badges one per badge, and a rotation
    /// raises both <c>move_item</c> and <c>rotate_item</c> because a rotation genuinely is a move.
    /// </remarks>
    ImmutableArray<ProgressSignal> Translate(TEvent e);

    /// <summary>
    /// The source event's operation id, when it has one.
    /// </summary>
    /// <remarks>
    /// Empty by default because almost nothing is replayable; the catalogue purchase is the one
    /// event that is, and its translator is the one override. The value ends up on the batch rather
    /// than on each signal, so a consumer spends its replay receipt once per source event.
    /// </remarks>
    string DeliveryIdOf(TEvent e) => string.Empty;
}
