using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Outgoing.Room.Session;

[GenerateSerializer, Immutable]
public sealed record RoomForwardMessageComposer : IComposer
{
    [Id(0)]
    public required RoomId RoomId { get; init; }
}
