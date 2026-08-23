using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Moderator;

namespace Vortex.Revisions.Revision20260701.Parsers.Moderator;

internal class DefaultSanctionMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new DefaultSanctionMessage
        {
            UserId = packet.PopInt(),
            TopicId = packet.PopInt(),
            Message = packet.PopString(),
            IssueId = packet.End ? -1 : packet.PopInt(),
        };
}
