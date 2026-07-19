using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.GroupForums;

internal class ModerateMessageMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ModerateMessageMessage
        {
            GroupId = packet.PopInt(),
            ThreadId = packet.PopInt(),
            MessageId = packet.PopInt(),
            Action = packet.PopInt(),
        };
}
