using Vortex.Primitives.Messages.Outgoing.Inventory.Pets;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets;

internal class PetAddedToInventoryEventMessageComposerSerializer(int header)
    : AbstractSerializer<PetAddedToInventoryEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PetAddedToInventoryEventMessageComposer message
    )
    {
        PetDataSerializer.Serialize(packet, message.Pet);
        packet.WriteBoolean(true);
    }
}
