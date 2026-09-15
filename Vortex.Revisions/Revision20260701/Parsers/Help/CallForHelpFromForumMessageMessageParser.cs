using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Help;

namespace Vortex.Revisions.Revision20260701.Parsers.Help;

internal class CallForHelpFromForumMessageMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int groupId = packet.PopInt();
        int threadId = packet.PopInt();
        int postId = packet.PopInt();
        int topicId = packet.PopInt();
        string message = packet.PopString();

        // `_SafeCls_3451` builds its array with `= [p1..p7]` rather than pushing, which is why the
        // spec scanner records it as writing nothing and the field-count check never saw these two.
        // They are filled for the client's unlawful report categories only.
        string reporterName = packet.PopString();
        string reporterEmail = packet.PopString();

        return new CallForHelpFromForumMessageMessage
        {
            GroupId = groupId,
            ThreadId = threadId,
            PostId = postId,
            TopicId = topicId,
            Message = message,
            ReporterName = reporterName,
            ReporterEmail = reporterEmail,
        };
    }
}
