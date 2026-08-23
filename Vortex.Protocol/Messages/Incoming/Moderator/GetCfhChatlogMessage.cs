using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Moderator;

public record GetCfhChatlogMessage : IMessageEvent
{
    public required int CallId { get; init; }
}
