using Vortex.Primitives.Messages.Outgoing.Inventory.Bots;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Bots;

internal class BotRemovedFromInventoryEventMessageComposerSerializer(int header)
    : AbstractSerializer<BotRemovedFromInventoryEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        BotRemovedFromInventoryEventMessageComposer message
    )
    {
        //
    }
}
