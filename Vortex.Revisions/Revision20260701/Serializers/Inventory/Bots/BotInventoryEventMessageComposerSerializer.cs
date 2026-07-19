using Vortex.Primitives.Messages.Outgoing.Inventory.Bots;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Bots;

internal class BotInventoryEventMessageComposerSerializer(int header)
    : AbstractSerializer<BotInventoryEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        BotInventoryEventMessageComposer message
    )
    {
        packet.WriteInteger(0);
    }
}
