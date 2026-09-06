using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// Completed trades, for both sides.
/// </summary>
/// <remarks>
/// Each side's fact is the other side: "trade with three different people" counts partners, which is
/// the only thing a trade filter can usefully be about.
/// </remarks>
public sealed class TradeTranslator : ISignalTranslator<TradeCompletedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.CompleteTrade, [Facts.Player])];

    public ImmutableArray<ProgressSignal> Translate(TradeCompletedEvent e) =>
        [
            new(
                e.PlayerOneId,
                SignalActions.CompleteTrade,
                1,
                Target: null,
                SignalFacts.Build().Id(Facts.Player, e.PlayerTwoId)
            ),
            new(
                e.PlayerTwoId,
                SignalActions.CompleteTrade,
                1,
                Target: null,
                SignalFacts.Build().Id(Facts.Player, e.PlayerOneId)
            ),
        ];
}
