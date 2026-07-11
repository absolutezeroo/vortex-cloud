using Turbo.Primitives.Messages.Incoming.Moderator;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Parsers.Moderator;

internal class ModBanMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int userId = packet.PopInt();
        string message = packet.PopString();
        int topicId = packet.PopInt();
        int sanctionTypeId = packet.PopInt();
        bool isSpecialFlag = packet.PopBoolean();
        int issueId = packet.End ? -1 : packet.PopInt();

        return new ModBanMessage
        {
            UserId = userId,
            Message = message,
            TopicId = topicId,
            SanctionTypeId = sanctionTypeId,
            IsSpecialFlag = isSpecialFlag,
            IssueId = issueId,
        };
    }
}
