using Vortex.Primitives.Messages.Outgoing.Inventory.Furni;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Inventory.Furni.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Furni;

internal class FurniListAddOrUpdateEventMessageComposerSerializer(int header)
    : AbstractSerializer<FurniListAddOrUpdateEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FurniListAddOrUpdateEventMessageComposer message
    )
    {
        packet.WriteInteger(1);

        FurnitureItemSerializer.Serialize(packet, message.Item);
    }
}
