using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Handshake;

public record CompleteDiffieHandshakeMessage : IMessageEvent
{
    public required string SharedKey { get; init; }
}
