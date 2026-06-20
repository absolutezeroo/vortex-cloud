using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.GroupForums;

internal class PostThreadMessageComposerSerializer(int header)
    : AbstractSerializer<PostThreadMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PostThreadMessageComposer message)
    {
        packet.WriteInteger(message.GroupId);

        ForumThreadSnapshot t = message.Thread;

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
