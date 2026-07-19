using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Outgoing.Room.Engine;

[GenerateSerializer, Immutable]
public sealed record RoomEntryInfoMessageComposer : IComposer
{
    [Id(0)]
    public required RoomId RoomId { get; init; }

    [Id(1)]
    public required bool IsOwner { get; init; }
}
