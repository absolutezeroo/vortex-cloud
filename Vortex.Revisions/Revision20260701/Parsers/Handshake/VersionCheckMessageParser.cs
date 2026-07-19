using Vortex.Primitives.Messages.Incoming.Handshake;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

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
