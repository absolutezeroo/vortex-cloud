using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record ApproveNameMessage : IMessageEvent
{
    public string Name { get; init; } = string.Empty;
    public int Mode { get; init; }
}
