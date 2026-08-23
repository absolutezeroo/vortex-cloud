using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Protocol.Messages.Outgoing.Room.Engine;

[GenerateSerializer, Immutable]
public sealed record ItemStateUpdateMessageComposer : IComposer
{
    [Id(0)]
    public required RoomObjectId ObjectId { get; init; }

    [Id(1)]
    public required string State { get; init; }
}
