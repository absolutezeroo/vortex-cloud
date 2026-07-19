using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Moderator;

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
