using Vortex.Primitives.Messages.Outgoing.Inventory.Badges;
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

        foreach (PlayerBadgeSnapshot badge in message.Badges)
        {
            packet.WriteInteger(badge.SlotId);
            packet.WriteString(badge.BadgeCode);
        }
    }
}
