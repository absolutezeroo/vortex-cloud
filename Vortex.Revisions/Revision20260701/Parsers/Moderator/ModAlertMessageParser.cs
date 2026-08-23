using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Moderator;

namespace Vortex.Revisions.Revision20260701.Parsers.Moderator;

internal class ModAlertMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ModAlertMessage
        {
            UserId = packet.PopInt(),
            Message = packet.PopString(),
            Topic = packet.PopInt(),
        };
}
