using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Moderator;

public record GetModeratorRoomInfoMessage : IMessageEvent
{
    public required int RoomId { get; init; }
}
