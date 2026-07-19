using Vortex.Primitives.Messages.Outgoing.Inventory.Badges;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Badges;

internal class BadgePointLimitsEventMessageComposerSerializer(int header)
    : AbstractSerializer<BadgePointLimitsEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        BadgePointLimitsEventMessageComposer message
    )
    {
        packet.WriteInteger(message.LimitsByBadgeCodePrefix.Count);

        foreach (BadgePointLimitGroup group in message.LimitsByBadgeCodePrefix)
        {
            packet.WriteString(group.BadgeCodePrefix).WriteInteger(group.Levels.Count);

            foreach (BadgePointLimitLevel level in group.Levels)
            {
                packet.WriteInteger(level.Level).WriteInteger(level.Limit);
            }
        }
    }
}
