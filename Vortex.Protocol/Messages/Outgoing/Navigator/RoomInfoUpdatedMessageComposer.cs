using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Protocol.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record RoomInfoUpdatedMessageComposer : IComposer
{
    [Id(0)]
    public required RoomId RoomId { get; init; }
}
