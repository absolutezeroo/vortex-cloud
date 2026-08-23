using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Nux;

/// <summary>
/// Picks the starter room. Carries the room TYPE the client offered (one of
/// <c>new.user.flow.roomTypes</c>, e.g. "10"), not a room id — the room does not exist yet.
/// </summary>
public record SelectInitialRoomMessage : IMessageEvent
{
    public required string RoomType { get; init; }
}
