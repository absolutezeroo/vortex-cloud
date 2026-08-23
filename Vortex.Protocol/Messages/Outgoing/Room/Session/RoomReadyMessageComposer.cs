using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Outgoing.Room.Session;

[GenerateSerializer, Immutable]
public sealed record RoomReadyMessageComposer : IComposer
{
    [Id(0)]
    public required string WorldType { get; init; }

    [Id(1)]
    public required RoomId RoomId { get; init; }
}
