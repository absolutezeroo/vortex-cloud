using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Bought a targeted offer.</summary>
public sealed class TargetedOfferTranslator : ISignalTranslator<TargetedOfferPurchasedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.BuyTargetedOffer,
            [Facts.Offer, Facts.Code, Facts.Quantity, Facts.Price],
            TargetKind: FactKind.OfferId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(TargetedOfferPurchasedEvent e) =>
        [
            new(
                e.PlayerId,
                SignalActions.BuyTargetedOffer,
                1,
                SignalValue.Id(e.OfferId),
                SignalFacts
                    .Build()
                    .Id(Facts.Offer, e.OfferId)
                    .Text(Facts.Code, e.Identifier)
                    .Number(Facts.Quantity, e.Quantity)
                    .Number(Facts.Price, e.CreditCost)
            ),
        ];
}
