using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Put an item up for sale on the marketplace.</summary>
public sealed class MarketplaceListedTranslator : ISignalTranslator<MarketplaceOfferListedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.ListOnMarketplace,
            [Facts.Definition, Facts.Price],
            TargetKind: FactKind.FurnitureId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(MarketplaceOfferListedEvent e) =>
        [
            new(
                e.SellerId.Value,
                SignalActions.ListOnMarketplace,
                1,
                SignalValue.Id(e.DefinitionId),
                SignalFacts
                    .Build()
                    .Id(Facts.Definition, e.DefinitionId)
                    .Number(Facts.Price, e.Price)
            ),
        ];
}
