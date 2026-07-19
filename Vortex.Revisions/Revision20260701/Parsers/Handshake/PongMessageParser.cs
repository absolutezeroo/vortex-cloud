using Vortex.Primitives.Messages.Incoming.Handshake;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Handshake;

internal class PongMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new PongMessage();
}
