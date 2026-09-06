using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Collected what the marketplace owed. The amount is the progress.</summary>
public sealed class MarketplaceRedeemedTranslator
    : ISignalTranslator<MarketplaceCreditsRedeemedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.RedeemMarketplaceCredits, [Facts.Price, Facts.Quantity])];

    public ImmutableArray<ProgressSignal> Translate(MarketplaceCreditsRedeemedEvent e) =>
        [
            new(
                e.SellerId.Value,
                SignalActions.RedeemMarketplaceCredits,
                e.Credits,
                Target: null,
                SignalFacts
                    .Build()
                    .Number(Facts.Price, e.Credits)
                    .Number(Facts.Quantity, e.OfferCount)
            ),
        ];
}
