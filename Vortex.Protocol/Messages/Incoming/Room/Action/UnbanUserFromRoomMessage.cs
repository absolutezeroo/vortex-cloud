using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Action;

public record UnbanUserFromRoomMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
