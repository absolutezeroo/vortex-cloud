using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Moderator;

public record GetCfhChatlogMessage : IMessageEvent
{
    public required int CallId { get; init; }
}
