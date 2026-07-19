using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.GroupForums;

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
