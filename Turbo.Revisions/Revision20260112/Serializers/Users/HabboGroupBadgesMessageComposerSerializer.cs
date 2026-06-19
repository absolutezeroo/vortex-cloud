using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Users;

internal class HabboGroupBadgesMessageComposerSerializer(int header)
    : AbstractSerializer<HabboGroupBadgesMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, HabboGroupBadgesMessageComposer message)
    {
        packet.WriteInteger(message.Badges.Count);
        foreach (var badge in message.Badges)
        {
            packet.WriteInteger(badge.GroupId);
            packet.WriteString(badge.BadgeCode);
        }
    }
}
