using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record PetBreedingEventMessageComposer : IComposer
{
    /// <summary>Where the pairing is up to. The client drives its breeding dialog off this.</summary>
    [Id(0)]
    public required int State { get; init; }

    [Id(1)]
    public required int OwnPetId { get; init; }

    [Id(2)]
    public required int OtherPetId { get; init; }
}
