using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Private messages the messenger accepted.</summary>
public sealed class MessengerTranslator : ISignalTranslator<MessengerMessageSentEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.SendMessengerMessage, [Facts.Player], TargetKind: FactKind.PlayerId)];

    public ImmutableArray<ProgressSignal> Translate(MessengerMessageSentEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.SendMessengerMessage,
                1,
                Target: SignalValue.Id(e.ReceiverId.Value),
                SignalFacts.Build().Id(Facts.Player, e.ReceiverId.Value)
            ),
        ];
}
