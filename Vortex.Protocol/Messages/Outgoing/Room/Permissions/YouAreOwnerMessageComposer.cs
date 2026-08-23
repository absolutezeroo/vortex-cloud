using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Outgoing.Room.Permissions;

[GenerateSerializer, Immutable]
public sealed record YouAreOwnerMessageComposer : IComposer
{
    [Id(0)]
    public required RoomId RoomId { get; init; }
}
