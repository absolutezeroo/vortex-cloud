using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Moderator;

public record GetUserChatlogMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
