using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Redeemed a clothing item from furniture.</summary>
public sealed class ClothingRedeemedTranslator : ISignalTranslator<ClothingRedeemedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.RedeemClothing, [Facts.Item], TargetKind: FactKind.OpaqueId)];

    public ImmutableArray<ProgressSignal> Translate(ClothingRedeemedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.RedeemClothing,
                1,
                SignalValue.Id(e.ItemId),
                SignalFacts.Build().Id(Facts.Item, e.ItemId)
            ),
        ];
}
