using Turbo.Primitives.Messages.Outgoing.Inventory.Badges;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Players.Snapshots;

namespace Turbo.Revisions.Revision20260701.Serializers.Inventory.Badges;

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
