using Vortex.Primitives.Messages.Incoming.Handshake;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Handshake;

internal class InfoRetrieveMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new InfoRetrieveMessage();
}
