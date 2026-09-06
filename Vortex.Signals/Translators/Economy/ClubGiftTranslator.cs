using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Claimed a club gift.</summary>
public sealed class ClubGiftTranslator : ISignalTranslator<ClubGiftClaimedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.ClaimClubGift, [Facts.ClubGift], TargetKind: FactKind.Text)];

    public ImmutableArray<ProgressSignal> Translate(ClubGiftClaimedEvent e) =>
        [
            new(
                e.PlayerId,
                SignalActions.ClaimClubGift,
                1,
                e.ProductCode,
                SignalFacts.Build().Text(Facts.ClubGift, e.ProductCode)
            ),
        ];
}
