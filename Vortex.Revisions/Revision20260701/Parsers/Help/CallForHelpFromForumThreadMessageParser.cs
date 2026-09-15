using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Help;

namespace Vortex.Revisions.Revision20260701.Parsers.Help;

internal class CallForHelpFromForumThreadMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int groupId = packet.PopInt();
        int threadId = packet.PopInt();
        int topicId = packet.PopInt();
        string message = packet.PopString();

        // Trailing pair, same as every other report variant (`_SafeCls_3708` = [p1..p6]).
        string reporterName = packet.PopString();
        string reporterEmail = packet.PopString();

        return new CallForHelpFromForumThreadMessage
        {
            GroupId = groupId,
            ThreadId = threadId,
            TopicId = topicId,
            Message = message,
            ReporterName = reporterName,
            ReporterEmail = reporterEmail,
        };
    }
}
