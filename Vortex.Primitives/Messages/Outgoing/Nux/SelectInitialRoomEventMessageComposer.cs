using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Outgoing.Nux;

/// <summary>
/// Answers <see cref="Incoming.Nux.SelectInitialRoomMessage"/> with the room that was created.
/// A <see cref="RoomId"/> greater than zero is what makes the client set it as its home room.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record SelectInitialRoomEventMessageComposer : IComposer
{
    [Id(0)]
    public required short Status { get; init; }

    [Id(1)]
    public required RoomId RoomId { get; init; }
}
