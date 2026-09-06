using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Entered an LTD raffle.</summary>
public sealed class RaffleEnteredTranslator : ISignalTranslator<LtdRaffleEnteredEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.EnterRaffle, [Facts.Price])];

    public ImmutableArray<ProgressSignal> Translate(LtdRaffleEnteredEvent e) =>
        [
            new(
                e.PlayerId,
                SignalActions.EnterRaffle,
                1,
                SignalValue.Id(e.SeriesId),
                SignalFacts.Build().Number(Facts.Price, e.Cost)
            ),
        ];
}
