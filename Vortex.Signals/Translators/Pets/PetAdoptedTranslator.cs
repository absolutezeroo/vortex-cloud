using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Adopted a pet. Its species and the name it was given both come free.</summary>
public sealed class PetAdoptedTranslator : ISignalTranslator<PetAdoptedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.AdoptPet,
            [Facts.Pet, Facts.PetType, Facts.GivenName],
            TargetKind: FactKind.OpaqueId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(PetAdoptedEvent e) =>
        [
            new(
                e.OwnerId.Value,
                SignalActions.AdoptPet,
                1,
                SignalValue.Id(e.PetId),
                SignalFacts
                    .Build()
                    .Id(Facts.Pet, e.PetId)
                    .Number(Facts.PetType, e.Type)
                    .Text(Facts.GivenName, e.Name)
            ),
        ];
}
