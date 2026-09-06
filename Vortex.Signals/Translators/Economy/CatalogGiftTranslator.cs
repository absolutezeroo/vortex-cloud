using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Bought something for somebody else. Credited to the buyer, with the receiver as a fact.</summary>
public sealed class CatalogGiftTranslator : ISignalTranslator<CatalogGiftPurchasedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.BuyGift,
            [Facts.Offer, Facts.Player, Facts.Price],
            TargetKind: FactKind.OfferId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(CatalogGiftPurchasedEvent e) =>
        [
            new(
                e.BuyerPlayerId,
                SignalActions.BuyGift,
                1,
                SignalValue.Id(e.OfferId),
                SignalFacts
                    .Build()
                    .Id(Facts.Offer, e.OfferId)
                    .Id(Facts.Player, e.ReceiverPlayerId)
                    .Number(Facts.Price, e.CreditCost)
            ),
        ];
}
