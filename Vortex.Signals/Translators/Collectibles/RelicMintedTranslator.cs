using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Minted a relic. Its serial within the series is what makes an early mint worth a task.</summary>
public sealed class RelicMintedTranslator : ISignalTranslator<RelicMintedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.MintRelic,
            [Facts.Definition, Facts.Serial, Facts.Price],
            TargetKind: FactKind.FurnitureId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(RelicMintedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.MintRelic,
                1,
                SignalValue.Id(e.DefinitionId),
                SignalFacts
                    .Build()
                    .Id(Facts.Definition, e.DefinitionId)
                    .Number(Facts.Serial, e.SerialNumber)
                    .Number(Facts.Price, e.StampCost)
            ),
        ];
}
