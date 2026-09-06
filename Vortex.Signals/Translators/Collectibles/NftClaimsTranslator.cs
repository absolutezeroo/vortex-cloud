using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Collected pending collectible claims. The amount is how many came in.</summary>
public sealed class NftClaimsTranslator : ISignalTranslator<NftClaimsCollectedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.CollectNftClaims, [Facts.Quantity])];

    public ImmutableArray<ProgressSignal> Translate(NftClaimsCollectedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.CollectNftClaims,
                e.Count,
                Target: null,
                SignalFacts.Build().Number(Facts.Quantity, e.Count)
            ),
        ];
}
