using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>A Habbicon arrived, however it arrived. Distinct from using one, which already existed.</summary>
public sealed class HabbiconGrantedTranslator : ISignalTranslator<HabbiconGrantedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.EarnHabbicon,
            [Facts.Habbicon, Facts.Collection],
            TargetKind: FactKind.OpaqueId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(HabbiconGrantedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.EarnHabbicon,
                1,
                SignalValue.Id(e.HabbiconId),
                SignalFacts
                    .Build()
                    .Id(Facts.Habbicon, e.HabbiconId)
                    .Id(Facts.Collection, e.CollectionId)
            ),
        ];
}
