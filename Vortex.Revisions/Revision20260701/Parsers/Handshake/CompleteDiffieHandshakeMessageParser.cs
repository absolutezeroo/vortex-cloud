using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Handshake;

namespace Vortex.Revisions.Revision20260701.Parsers.Handshake;

internal class CompleteDiffieHandshakeMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new CompleteDiffieHandshakeMessage { SharedKey = packet.PopString() };
}
