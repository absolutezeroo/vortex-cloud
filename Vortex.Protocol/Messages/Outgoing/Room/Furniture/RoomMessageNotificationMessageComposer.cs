using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Furniture;

/// <summary>
/// Unread guestbook/post-it messages waiting in one of the player's rooms (header 1740).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_2942/_SafeCls_3790.as): an int, a string, an int.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RoomMessageNotificationMessageComposer : IComposer
{
    [Id(0)]
    public required int RoomId { get; init; }

    [Id(1)]
    public required string RoomName { get; init; }

    [Id(2)]
    public required int MessageCount { get; init; }
}
