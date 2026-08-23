using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Pets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets;

internal class PetRemovedFromInventoryEventMessageComposerSerializer(int header)
    : AbstractSerializer<PetRemovedFromInventoryEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PetRemovedFromInventoryEventMessageComposer message
    )
    {
        packet.WriteInteger(message.PetId);
    }
}
