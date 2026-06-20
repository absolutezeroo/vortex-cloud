using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Groupforums;

internal class ForumThreadsMessageComposerSerializer(int header)
    : AbstractSerializer<ForumThreadsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ForumThreadsMessageComposer message)
    {
        var page = message.Page;

        packet.WriteInteger(page.GroupId);
        packet.WriteInteger(page.StartIndex);

        packet.WriteInteger(page.Threads.Count);
        foreach (var t in page.Threads)
        {
            packet.WriteInteger(t.ThreadId);
            packet.WriteInteger(t.AuthorId);
            packet.WriteString(t.AuthorName);
            packet.WriteString(t.Subject);
            packet.WriteBoolean(t.IsSticky);
            packet.WriteBoolean(t.IsLocked);
            packet.WriteInteger(t.CreationTimeAsSecondsAgo);
            packet.WriteInteger(t.MessageCount);
            packet.WriteInteger(t.UnreadMessageCount);
            packet.WriteInteger(t.LastMessageId);
            packet.WriteInteger(t.LastMessageAuthorId);
            packet.WriteString(t.LastMessageAuthorName);
            packet.WriteInteger(t.LastMessageTimeAsSecondsAgo);
            packet.WriteByte((byte)t.State);
            packet.WriteInteger(t.AdminId);
            packet.WriteString(t.AdminName);
            packet.WriteInteger(t.AdminOperationTimeAsSecondsAgo);
        }
    }
}
