using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Bought from the collectibles store.</summary>
public sealed class NftStoreTranslator : ISignalTranslator<NftStorePurchasedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.BuyFromNftStore, [Facts.Code, Facts.Price], TargetKind: FactKind.Text)];

    public ImmutableArray<ProgressSignal> Translate(NftStorePurchasedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.BuyFromNftStore,
                1,
                e.ProductCode,
                SignalFacts.Build().Text(Facts.Code, e.ProductCode).Number(Facts.Price, e.Price)
            ),
        ];
}
