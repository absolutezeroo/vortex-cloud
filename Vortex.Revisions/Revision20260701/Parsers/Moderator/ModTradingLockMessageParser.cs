using Vortex.Protocol.Messages.Incoming.Moderator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Moderator;

internal class ModTradingLockMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        // _SafeCls_3651(userId, message, durationMinutes, topicId, [issueId]) — duration comes
        // before the topic. Swapping the two fed the topic id into the lock-length lookup.
        int userId = packet.PopInt();
        string message = packet.PopString();
        int durationMinutes = packet.PopInt();
        int topicId = packet.PopInt();
        int issueId = packet.End ? -1 : packet.PopInt();

        return new ModTradingLockMessage
        {
            UserId = userId,
            Message = message,
            DurationMinutes = durationMinutes,
            TopicId = topicId,
            IssueId = issueId,
        };
    }
}
