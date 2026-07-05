using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Moderator;

public record GetCfhChatlogMessage : IMessageEvent
{
    public required int CallId { get; init; }
}
