using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Put a pet down in a room.</summary>
public sealed class PetPlacedTranslator : ISignalTranslator<PetPlacedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.PlacePet, [Facts.Pet, Facts.Room], TargetKind: FactKind.OpaqueId)];

    public ImmutableArray<ProgressSignal> Translate(PetPlacedEvent e) =>
        [
            new(
                e.ActorId.Value,
                SignalActions.PlacePet,
                1,
                SignalValue.Id(e.PetId),
                SignalFacts.Build().Id(Facts.Pet, e.PetId).Id(Facts.Room, e.RoomId)
            ),
        ];
}
