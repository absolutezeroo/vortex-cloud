using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

public record IgnoreUserMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
