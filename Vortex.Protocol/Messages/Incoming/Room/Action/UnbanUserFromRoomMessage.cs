using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Action;

public record UnbanUserFromRoomMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
