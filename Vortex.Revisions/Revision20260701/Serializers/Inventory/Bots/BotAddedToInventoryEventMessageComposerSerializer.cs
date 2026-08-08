using Vortex.Primitives.Messages.Outgoing.Inventory.Bots;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Bots;

internal class BotAddedToInventoryEventMessageComposerSerializer(int header)
    : AbstractSerializer<BotAddedToInventoryEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        BotAddedToInventoryEventMessageComposer message
    )
    {
        BotSerialization.WriteBot(packet, message.Bot);

        packet.WriteBoolean(message.OpenInventory);
    }
}
