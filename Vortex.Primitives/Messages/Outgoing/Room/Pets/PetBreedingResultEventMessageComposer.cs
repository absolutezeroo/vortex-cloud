using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetBreedingResultEventMessageComposer : IComposer
{
    [Id(0)]
    public required int PetOneId { get; init; }

    [Id(1)]
    public required int PetTwoId { get; init; }

    [Id(2)]
    public required int Result { get; init; }
}
