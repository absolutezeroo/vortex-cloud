using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Bought or renewed club. The amount is the months, for a Highest-mode task on membership.</summary>
public sealed class ClubPurchasedTranslator : ISignalTranslator<ClubPurchasedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.BuyClub, [Facts.Months, Facts.Price])];

    public ImmutableArray<ProgressSignal> Translate(ClubPurchasedEvent e) =>
        [
            new(
                e.PlayerId,
                SignalActions.BuyClub,
                e.Months,
                Target: null,
                SignalFacts
                    .Build()
                    .Number(Facts.Months, e.TotalMonths)
                    .Number(Facts.Price, e.CreditCost)
            ),
        ];
}
