using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Handshake;

namespace Vortex.Revisions.Revision20260701.Parsers.Handshake;

internal class PongMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new PongMessage();
}
