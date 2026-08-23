using Orleans;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Inventory.Furni;

[GenerateSerializer, Immutable]
public sealed record FurniListAddOrUpdateEventMessageComposer : IComposer
{
    [Id(0)]
    public required FurnitureItemSnapshot Item { get; init; }
}
