using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record RoomEventCancelMessageComposer : IComposer
{
    [Id(0)]
    public required RoomId RoomId { get; init; }
}
