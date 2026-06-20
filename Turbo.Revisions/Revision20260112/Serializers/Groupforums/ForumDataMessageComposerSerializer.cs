using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.GroupForums;

internal class ForumDataMessageComposerSerializer(int header)
    : AbstractSerializer<ForumDataMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ForumDataMessageComposer message)
    {
        ForumSnapshot f = message.Forum;

        packet.WriteInteger(f.GroupId);
        packet.WriteString(f.Name);
        packet.WriteString(f.Description);
        packet.WriteString(f.Icon);
        packet.WriteInteger(f.TotalThreads);
        packet.WriteInteger(f.LeaderboardScore);
        packet.WriteInteger(f.TotalMessages);
        packet.WriteInteger(f.UnreadMessages);
        packet.WriteInteger(f.LastMessageId);
        packet.WriteInteger(f.LastMessageAuthorId);
        packet.WriteString(f.LastMessageAuthorName);
        packet.WriteInteger(f.LastMessageTimeAsSecondsAgo);

        packet.WriteInteger(f.ReadPermissions);
        packet.WriteInteger(f.PostMessagePermissions);
        packet.WriteInteger(f.PostThreadPermissions);
        packet.WriteInteger(f.ModeratePermissions);
        packet.WriteString(f.ReadPermissionError);
        packet.WriteString(f.PostMessagePermissionError);
        packet.WriteString(f.PostThreadPermissionError);
        packet.WriteString(f.ModeratePermissionError);
        packet.WriteString(f.ReportPermissionError);
        packet.WriteBoolean(f.CanChangeSettings);
        packet.WriteBoolean(f.IsStaff);
    }
}
