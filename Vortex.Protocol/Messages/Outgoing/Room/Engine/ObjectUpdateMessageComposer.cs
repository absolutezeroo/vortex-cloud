using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Protocol.Messages.Outgoing.Room.Engine;

[GenerateSerializer, Immutable]
public sealed record ObjectUpdateMessageComposer : IComposer
{
    [Id(0)]
    public required RoomFloorItemSnapshot FloorItem { get; init; }
}
