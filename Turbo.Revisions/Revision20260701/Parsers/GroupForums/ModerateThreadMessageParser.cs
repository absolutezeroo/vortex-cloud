using Turbo.Primitives.Messages.Incoming.GroupForums;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Parsers.GroupForums;

internal class ModerateThreadMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ModerateThreadMessage
        {
            GroupId = packet.PopInt(),
            ThreadId = packet.PopInt(),
            Action = packet.PopInt(),
        };
}
