using Vortex.Primitives.Messages.Outgoing.Inventory.Pets;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets;

internal class PetInventoryEventMessageComposerSerializer(int header)
    : AbstractSerializer<PetInventoryEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PetInventoryEventMessageComposer message
    )
    {
        packet.WriteInteger(1).WriteInteger(0).WriteInteger(message.Pets.Length);

        foreach (PetSnapshot pet in message.Pets)
        {
            PetDataSerializer.Serialize(packet, pet);
        }
    }
}
