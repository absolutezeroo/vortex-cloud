using Turbo.Primitives.Messages.Incoming.GroupForums;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Groupforums;

internal class UpdateForumReadMarkerMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int count = packet.PopInt();
        for (int i = 0; i < count; i++)
        {
            packet.PopInt();
            packet.PopInt();
            packet.PopBoolean();
        }

        return new UpdateForumReadMarkerMessage();
    }
}
