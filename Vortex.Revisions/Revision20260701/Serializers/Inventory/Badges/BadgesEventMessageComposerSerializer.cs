using Vortex.Protocol.Messages.Outgoing.Inventory.Badges;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Badges;

internal class BadgesEventMessageComposerSerializer(int header)
    : AbstractSerializer<BadgesEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, BadgesEventMessageComposer message)
    {
        packet.WriteInteger(1); // totalFragments
        packet.WriteInteger(1); // fragmentNo
        packet.WriteInteger(message.Badges.Length);

        // Four fields per badge, not two: WIN63's parser
        // (unknowns/_SafePkg_3206/_SafeCls_3564.as) reads slotId, badgeCode, ownerCount and
        // badgeRarityId, and builds a Badge from all four. Sending only the first two left every
        // badge with rarity 0, which is what the client's rarity overlay tests.
        foreach (PlayerBadgeSnapshot badge in message.Badges)
        {
            packet.WriteInteger(badge.SlotId);
            packet.WriteString(badge.BadgeCode);
            packet.WriteInteger(badge.OwnerCount);
            packet.WriteInteger(badge.BadgeRarityId);
        }
    }
}
