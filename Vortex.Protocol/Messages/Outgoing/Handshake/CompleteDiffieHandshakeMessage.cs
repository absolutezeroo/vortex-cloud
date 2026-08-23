using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Handshake;

public sealed record CompleteDiffieHandshakeMessageComposer : IComposer
{
    public required string PublicKey { get; init; }
    public bool ServerClientEncryption { get; init; }
}
