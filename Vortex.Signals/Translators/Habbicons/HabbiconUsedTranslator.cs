using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// Habbicons used.
/// </summary>
/// <remarks>
/// The room is omitted rather than emitted as zero when the Habbicon was used in a private
/// conversation. A filter fails closed on an absent fact, so "in any room but yours" correctly does
/// not match a private conversation — which <c>room = 0</c> would have matched.
/// </remarks>
public sealed class HabbiconUsedTranslator : ISignalTranslator<HabbiconUsedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.UseHabbicon, [Facts.Habbicon, Facts.Room], TargetKind: FactKind.OpaqueId)];

    public ImmutableArray<ProgressSignal> Translate(HabbiconUsedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.UseHabbicon,
                1,
                Target: SignalValue.Id(e.HabbiconId),
                SignalFacts.Build().Id(Facts.Habbicon, e.HabbiconId).IdIfAny(Facts.Room, e.RoomId)
            ),
        ];
}
