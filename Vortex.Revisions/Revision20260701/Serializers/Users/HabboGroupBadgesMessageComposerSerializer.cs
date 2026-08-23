using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Users;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class HabboGroupBadgesMessageComposerSerializer(int header)
    : AbstractSerializer<HabboGroupBadgesMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, HabboGroupBadgesMessageComposer message)
    {
        packet.WriteInteger(message.Badges.Count);
        foreach (GroupBadgeSnapshot badge in message.Badges)
        {
            packet.WriteInteger(badge.GroupId);
            packet.WriteString(badge.BadgeCode);
        }
    }
}
