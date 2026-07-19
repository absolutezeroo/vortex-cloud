using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.GroupForums;

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
