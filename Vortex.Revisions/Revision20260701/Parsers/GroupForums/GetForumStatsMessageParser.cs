using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.GroupForums;

namespace Vortex.Revisions.Revision20260701.Parsers.GroupForums;

internal class GetForumStatsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetForumStatsMessage { GroupId = packet.PopInt() };
}
