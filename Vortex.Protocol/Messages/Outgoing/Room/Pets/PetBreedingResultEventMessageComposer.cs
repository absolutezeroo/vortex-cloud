using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Protocol.Messages.Outgoing.Room.Pets;

/// <summary>
/// The two outcomes the breeding dialog lays out side by side. The client parses the same seven-field
/// block twice, so both are always present.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record PetBreedingResultEventMessageComposer : IComposer
{
    [Id(0)]
    public required PetBreedingOutcomeSnapshot Result { get; init; }

    [Id(1)]
    public required PetBreedingOutcomeSnapshot OtherResult { get; init; }
}
