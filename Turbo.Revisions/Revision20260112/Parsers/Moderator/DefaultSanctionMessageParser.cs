using Turbo.Primitives.Messages.Incoming.Moderator;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Moderator;

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
