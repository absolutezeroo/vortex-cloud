using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

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
