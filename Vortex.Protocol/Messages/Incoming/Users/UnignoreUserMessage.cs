using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record UnignoreUserMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
