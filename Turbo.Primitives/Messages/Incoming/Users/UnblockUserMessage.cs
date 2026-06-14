using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Users;

public record UnblockUserMessage : IMessageEvent
{
    public required int PlayerId { get; init; }
}
