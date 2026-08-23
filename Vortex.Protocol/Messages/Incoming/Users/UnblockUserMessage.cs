using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

public record UnblockUserMessage : IMessageEvent
{
    public required int PlayerId { get; init; }
}
