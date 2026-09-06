using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Opened a mystery trophy. The prize furniture is filterable.</summary>
public sealed class MysteryTrophyTranslator : ISignalTranslator<MysteryTrophyOpenedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.OpenMysteryTrophy,
            [Facts.Definition, Facts.Room],
            TargetKind: FactKind.FurnitureId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(MysteryTrophyOpenedEvent e) =>
        [
            new(
                e.PlayerId,
                SignalActions.OpenMysteryTrophy,
                1,
                SignalValue.Id(e.PrizeFurnitureDefinitionId),
                SignalFacts
                    .Build()
                    .IdIfAny(Facts.Definition, e.PrizeFurnitureDefinitionId)
                    .Id(Facts.Room, e.RoomId)
            ),
        ];
}
