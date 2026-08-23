using Vortex.Protocol.Messages.Incoming.Help;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Help;

internal class CallForHelpFromForumThreadMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int groupId = packet.PopInt();
        int threadId = packet.PopInt();
        int topicId = packet.PopInt();
        string message = packet.PopString();

        return new CallForHelpFromForumThreadMessage
        {
            GroupId = groupId,
            ThreadId = threadId,
            TopicId = topicId,
            Message = message,
        };
    }
}
