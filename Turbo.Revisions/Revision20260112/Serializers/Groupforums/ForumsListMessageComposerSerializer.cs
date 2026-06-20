using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Groupforums;

internal class ForumsListMessageComposerSerializer(int header)
    : AbstractSerializer<ForumsListMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ForumsListMessageComposer message)
    {
        ForumsListPageSnapshot page = message.Page;

        packet.WriteInteger(page.ListCode);
        packet.WriteInteger(page.TotalAmount);
        packet.WriteInteger(page.StartIndex);

        packet.WriteInteger(page.Forums.Count);
        foreach (ForumSnapshot f in page.Forums)
        {
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
        }
    }
}
