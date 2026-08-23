using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Handshake;

public record SSOTicketMessage : IMessageEvent
{
    public required string SSO { get; init; }
    public int ElapsedMilliseconds { get; init; }
}
