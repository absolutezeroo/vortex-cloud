using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record PetInventoryEventMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<PetSnapshot> Pets { get; init; }
}
