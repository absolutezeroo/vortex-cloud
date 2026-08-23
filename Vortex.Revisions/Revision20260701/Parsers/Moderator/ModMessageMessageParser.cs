using Vortex.Protocol.Messages.Incoming.Moderator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Moderator;

internal class ModMessageMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int userId = packet.PopInt();
        string message = packet.PopString();

        // _SafeCls_3552 pushes two literal "" between the message and the topic id.
        packet.PopString();
        packet.PopString();

        int topicId = packet.PopInt();
        int issueId = packet.End ? -1 : packet.PopInt();

        return new ModMessageMessage
        {
            UserId = userId,
            Message = message,
            TopicId = topicId,
            IssueId = issueId,
        };
    }
}
