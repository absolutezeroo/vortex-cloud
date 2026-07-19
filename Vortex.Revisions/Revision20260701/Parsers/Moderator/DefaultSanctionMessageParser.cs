using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

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
