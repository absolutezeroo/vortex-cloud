using Vortex.Primitives.Messages.Outgoing.Inventory.Badges;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Badges;

internal class BadgeReceivedEventMessageComposerSerializer(int header)
    : AbstractSerializer<BadgeReceivedEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        BadgeReceivedEventMessageComposer message
    )
    {
        packet.WriteInteger(message.SlotId);
        packet.WriteString(message.BadgeCode);
    }
}
