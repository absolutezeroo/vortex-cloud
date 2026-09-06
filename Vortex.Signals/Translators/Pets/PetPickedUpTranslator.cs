using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Took a pet back out of a room.</summary>
public sealed class PetPickedUpTranslator : ISignalTranslator<PetPickedUpEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.PickUpPet, [Facts.Pet, Facts.Room], TargetKind: FactKind.OpaqueId)];

    public ImmutableArray<ProgressSignal> Translate(PetPickedUpEvent e) =>
        [
            new(
                e.ActorId.Value,
                SignalActions.PickUpPet,
                1,
                SignalValue.Id(e.PetId),
                SignalFacts.Build().Id(Facts.Pet, e.PetId).Id(Facts.Room, e.RoomId)
            ),
        ];
}
