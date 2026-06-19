using Turbo.Primitives.Messages.Incoming.Groupforums;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Groupforums;

internal class UpdateForumReadMarkerMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        var count = packet.PopInt();
        for (var i = 0; i < count; i++)
        {
            packet.PopInt();
            packet.PopInt();
            packet.PopBoolean();
        }

        return new UpdateForumReadMarkerMessage();
    }
}
