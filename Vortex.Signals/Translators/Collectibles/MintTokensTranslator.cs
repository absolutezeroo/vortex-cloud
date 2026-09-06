using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Bought minting tokens. The amount is how many, so "buy 50" is one task.</summary>
public sealed class MintTokensTranslator : ISignalTranslator<MintTokensPurchasedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.BuyMintTokens, [Facts.Quantity, Facts.Price])];

    public ImmutableArray<ProgressSignal> Translate(MintTokensPurchasedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.BuyMintTokens,
                e.Quantity,
                Target: null,
                SignalFacts.Build().Number(Facts.Quantity, e.Quantity).Number(Facts.Price, e.Cost)
            ),
        ];
}
