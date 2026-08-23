using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Handshake;

namespace Vortex.Revisions.Revision20260701.Parsers.Handshake;

internal class VersionCheckMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new VersionCheckMessage
        {
            ClientID = packet.PopInt(),
            ClientURL = packet.PopString(),
            ExternalVariablesURL = packet.PopString(),
        };
}
