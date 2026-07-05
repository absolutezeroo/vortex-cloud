using Turbo.Primitives.Messages.Incoming.Moderator;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Moderator;

internal class ModTradingLockMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int userId = packet.PopInt();
        string message = packet.PopString();
        int topicId = packet.PopInt();
        int lockDurationTypeId = packet.PopInt();
        int issueId = packet.End ? -1 : packet.PopInt();

        return new ModTradingLockMessage
        {
            UserId = userId,
            Message = message,
            TopicId = topicId,
            LockDurationTypeId = lockDurationTypeId,
            IssueId = issueId,
        };
    }
}
