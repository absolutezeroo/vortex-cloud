using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.GroupForums;

internal class UpdateMessageMessageComposerSerializer(int header)
    : AbstractSerializer<UpdateMessageMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UpdateMessageMessageComposer message)
    {
        packet.WriteInteger(message.GroupId);
        packet.WriteInteger(message.ThreadId);

        ForumPostSnapshot p = message.Post;

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
