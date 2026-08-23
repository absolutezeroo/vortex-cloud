using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record BlockUserMessage : IMessageEvent
{
    public required int PlayerId { get; init; }
}
