using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record PetAddedToInventoryEventMessageComposer : IComposer
{
    [Id(0)]
    public required PetSnapshot Pet { get; init; }
}
