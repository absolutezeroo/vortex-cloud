using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// A marketplace sale went through.
/// </summary>
/// <remarks>
/// Credited to the buyer, who is the one who acted. The seller's half arrives when they collect
/// what they are owed, which is its own event and its own action.
/// </remarks>
public sealed class MarketplaceBoughtTranslator : ISignalTranslator<MarketplaceOfferBoughtEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.BuyOnMarketplace,
            [Facts.Definition, Facts.Price, Facts.Player],
            TargetKind: FactKind.FurnitureId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(MarketplaceOfferBoughtEvent e) =>
        [
            new(
                e.BuyerId.Value,
                SignalActions.BuyOnMarketplace,
                1,
                SignalValue.Id(e.DefinitionId),
                SignalFacts
                    .Build()
                    .Id(Facts.Definition, e.DefinitionId)
                    .Number(Facts.Price, e.Price)
                    .Id(Facts.Player, e.SellerId.Value)
            ),
        ];
}
