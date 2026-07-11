using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.GroupForums;

internal class ThreadMessagesMessageComposerSerializer(int header)
    : AbstractSerializer<ThreadMessagesMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ThreadMessagesMessageComposer message)
    {
        ThreadMessagesPageSnapshot page = message.Page;

        packet.WriteInteger(page.GroupId);
        packet.WriteInteger(page.ThreadId);
        packet.WriteInteger(page.StartIndex);

        packet.WriteInteger(page.Messages.Count);
        foreach (ForumPostSnapshot p in page.Messages)
        {
            packet.WriteInteger(p.MessageId);
            packet.WriteInteger(p.MessageIndex);
            packet.WriteInteger(p.AuthorId);
            packet.WriteString(p.AuthorName);
            packet.WriteString(p.AuthorFigure);
            packet.WriteInteger(p.CreationTimeAsSecondsAgo);
            packet.WriteString(p.MessageText);
            packet.WriteByte((byte)p.State);
            packet.WriteInteger(p.AdminId);
            packet.WriteString(p.AdminName);
            packet.WriteInteger(p.AdminOperationTimeAsSecondsAgo);
            packet.WriteInteger(p.AuthorPostCount);
        }
    }
}
