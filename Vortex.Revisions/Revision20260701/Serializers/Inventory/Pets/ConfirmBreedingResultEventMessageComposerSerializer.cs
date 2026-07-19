using Vortex.Primitives.Messages.Outgoing.Inventory.Pets;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets;

internal class ConfirmBreedingResultEventMessageComposerSerializer(int header)
    : AbstractSerializer<ConfirmBreedingResultEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ConfirmBreedingResultEventMessageComposer message
    )
    {
        packet.WriteBoolean(message.Success).WriteInteger(message.NewPetId);
    }
}
