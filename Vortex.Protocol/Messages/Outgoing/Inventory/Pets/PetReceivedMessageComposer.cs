using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Protocol.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record PetReceivedMessageComposer : IComposer
{
    [Id(0)]
    public bool BoughtAsGift { get; init; }

    [Id(1)]
    public required PetSnapshot Pet { get; init; }
}
