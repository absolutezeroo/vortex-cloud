using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Claimed a completed collection's bonus Habbicon.</summary>
public sealed class HabbiconRewardTranslator
    : ISignalTranslator<HabbiconCollectionRewardClaimedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.ClaimHabbiconReward,
            [Facts.Collection, Facts.Habbicon],
            TargetKind: FactKind.Text
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(HabbiconCollectionRewardClaimedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.ClaimHabbiconReward,
                1,
                e.CollectionCode,
                SignalFacts
                    .Build()
                    .Text(Facts.Collection, e.CollectionCode)
                    .Id(Facts.Habbicon, e.RewardHabbiconId)
            ),
        ];
}
