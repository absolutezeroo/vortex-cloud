using Vortex.Primitives.Messages.Incoming.Handshake;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Handshake;

internal class DisconnectMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new DisconnectMessage();
}
