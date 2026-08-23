using Vortex.Protocol.Messages.Outgoing.Inventory.Pets;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets;

internal class PetReceivedMessageComposerSerializer(int header)
    : AbstractSerializer<PetReceivedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PetReceivedMessageComposer message)
    {
        packet.WriteBoolean(message.BoughtAsGift);

        PetDataSerializer.Serialize(packet, message.Pet);
    }
}
