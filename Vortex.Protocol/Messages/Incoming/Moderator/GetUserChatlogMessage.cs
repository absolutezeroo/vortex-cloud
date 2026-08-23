using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Moderator;

public record GetUserChatlogMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
