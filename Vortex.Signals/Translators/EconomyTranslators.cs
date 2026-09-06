using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// Catalogue purchases. Two actions from one event: one counting purchases, one counting the credits
/// they cost — which is how "spend 100 credits" is a task without a spending event of its own.
/// </summary>
/// <remarks>
/// The only translator that overrides <see cref="DeliveryIdOf"/>. The commerce relay delivers at
/// least once by design, and advancing a task twice for one purchase is the silent wrongness the
/// relay exists to avoid causing — so the batch carries the operation id and each consumer spends
/// its own replay receipt once for the whole purchase, exactly as the handler did before it.
/// </remarks>
public sealed class CatalogPurchaseTranslator : ISignalTranslator<CatalogPurchasedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(SignalActions.BuyFromCatalogue, [Facts.Offer], TargetKind: FactKind.OfferId),
        new(SignalActions.SpendCredits, []),
    ];

    public string DeliveryIdOf(CatalogPurchasedEvent e) => e.OperationId;

    public ImmutableArray<ProgressSignal> Translate(CatalogPurchasedEvent e)
    {
        ProgressSignal bought = new(
            e.PlayerId,
            SignalActions.BuyFromCatalogue,
            e.Quantity > 0 ? e.Quantity : 1,
            SignalValue.Id(e.OfferId),
            SignalFacts.Build().Id(Facts.Offer, e.OfferId)
        );

        // A free offer is still a purchase, but it is not a spend: a "spend credits" task must not
        // advance by zero, and an amount of zero would be a signal that says nothing happened.
        return e.CreditCost > 0
            ? [bought, new(e.PlayerId, SignalActions.SpendCredits, e.CreditCost, Target: null, [])]
            : [bought];
    }
}

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
