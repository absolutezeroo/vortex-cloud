using Vortex.Primitives.Packets;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Users;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class HabboUserBadgesMessageComposerSerializer(int header)
    : AbstractSerializer<HabboUserBadgesMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, HabboUserBadgesMessageComposer message)
    {
        // userId, then count and each equipped badge as (slotId, badgeCode, ownerCount,
        // badgeRarityId). The last two were missing: the client's parser
        // (WIN63 unknowns/_SafePkg_1891/_SafeCls_2978.as) reads four fields per badge and its
        // handler feeds all four to BadgesModel.updateBadge(), so sending two left the inventory
        // unable to wire this message at all without zeroing the rarity BadgesEvent had set.
        // win63_version's decompile of the same parser shows only two fields — it is the known
        // bad decompile; the primary tree settles it.
        packet.WriteInteger(message.UserId).WriteInteger(message.Badges.Length);

        foreach (PlayerBadgeSnapshot badge in message.Badges)
        {
            packet
                .WriteInteger(badge.SlotId)
                .WriteString(badge.BadgeCode)
                .WriteInteger(badge.OwnerCount)
                .WriteInteger(badge.BadgeRarityId);
        }
    }
}
