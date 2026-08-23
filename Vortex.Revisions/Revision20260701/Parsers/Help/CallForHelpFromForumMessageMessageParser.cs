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

        return new CallForHelpFromForumMessageMessage
        {
            GroupId = groupId,
            ThreadId = threadId,
            PostId = postId,
            TopicId = topicId,
            Message = message,
        };
    }
}
