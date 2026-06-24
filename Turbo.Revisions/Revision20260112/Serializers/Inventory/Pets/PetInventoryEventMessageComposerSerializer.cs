using Turbo.Primitives.Messages.Outgoing.Inventory.Pets;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Pets.Snapshots;
using Turbo.Revisions.Revision20260112.Serializers.Inventory.Pets.Data;

namespace Turbo.Revisions.Revision20260112.Serializers.Inventory.Pets;

internal class PetInventoryEventMessageComposerSerializer(int header)
    : AbstractSerializer<PetInventoryEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PetInventoryEventMessageComposer message
    )
    {
        packet.WriteInteger(1).WriteInteger(message.Pets.Length);

        foreach (PetSnapshot pet in message.Pets)
        {
            PetDataSerializer.Serialize(packet, pet);
        }
    }
}
