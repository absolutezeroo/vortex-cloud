using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Furni;

[GenerateSerializer, Immutable]
public sealed record FurniListRemoveEventMessageComposer : IComposer
{
    [Id(0)]
    public required RoomObjectId ItemId { get; init; }
}
