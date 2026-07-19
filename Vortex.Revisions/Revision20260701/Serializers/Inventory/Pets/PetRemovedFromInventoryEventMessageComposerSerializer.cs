using Vortex.Primitives.Messages.Outgoing.Inventory.Pets;
using Vortex.Primitives.Packets;

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
