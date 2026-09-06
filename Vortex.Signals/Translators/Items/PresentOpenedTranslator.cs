using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Unwrapped a present.</summary>
public sealed class PresentOpenedTranslator : ISignalTranslator<PresentOpenedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.OpenPresent, [Facts.Offer, Facts.Room], TargetKind: FactKind.OfferId)];

    public ImmutableArray<ProgressSignal> Translate(PresentOpenedEvent e) =>
        [
            new(
                e.ActorPlayerId,
                SignalActions.OpenPresent,
                1,
                SignalValue.Id(e.OfferId),
                SignalFacts.Build().IdIfAny(Facts.Offer, e.OfferId).Id(Facts.Room, e.RoomId)
            ),
        ];
}
