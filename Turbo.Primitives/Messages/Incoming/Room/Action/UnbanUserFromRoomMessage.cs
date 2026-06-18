using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Room.Action;

public record UnbanUserFromRoomMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
