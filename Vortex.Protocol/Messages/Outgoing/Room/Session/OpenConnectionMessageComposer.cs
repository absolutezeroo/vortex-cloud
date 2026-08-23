using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Protocol.Messages.Outgoing.Room.Session;

[GenerateSerializer, Immutable]
public sealed record OpenConnectionMessageComposer : IComposer
{
    [Id(0)]
    public required RoomId RoomId { get; init; }
}
