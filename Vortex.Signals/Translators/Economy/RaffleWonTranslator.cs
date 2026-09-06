using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Won an LTD raffle. The serial is the bragging right, so it is filterable.</summary>
public sealed class RaffleWonTranslator : ISignalTranslator<LtdRaffleWonEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.WinRaffle,
            [Facts.Definition, Facts.Serial],
            TargetKind: FactKind.FurnitureId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(LtdRaffleWonEvent e) =>
        [
            new(
                e.PlayerId,
                SignalActions.WinRaffle,
                1,
                SignalValue.Id(e.FurniDefinitionId),
                SignalFacts
                    .Build()
                    .Id(Facts.Definition, e.FurniDefinitionId)
                    .Number(Facts.Serial, e.SerialNumber)
            ),
        ];
}
