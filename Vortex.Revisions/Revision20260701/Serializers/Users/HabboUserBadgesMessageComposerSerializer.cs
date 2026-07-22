using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class HabboUserBadgesMessageComposerSerializer(int header)
    : AbstractSerializer<HabboUserBadgesMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, HabboUserBadgesMessageComposer message)
    {
        // userId, then count and each equipped badge as (slotId, badgeCode) — matches
        // HabboUserBadgesMessageParser / the profile badge grid.
        packet.WriteInteger(message.UserId).WriteInteger(message.Badges.Length);

        foreach (PlayerBadgeSnapshot badge in message.Badges)
        {
            packet.WriteInteger(badge.SlotId).WriteString(badge.BadgeCode);
        }
    }
}
