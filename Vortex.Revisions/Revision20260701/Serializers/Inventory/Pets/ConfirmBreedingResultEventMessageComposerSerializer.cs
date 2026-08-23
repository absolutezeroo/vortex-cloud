using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Pets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Pets.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets;

internal class ConfirmBreedingResultEventMessageComposerSerializer(int header)
    : AbstractSerializer<ConfirmBreedingResultEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ConfirmBreedingResultEventMessageComposer message
    )
    {
        packet.WriteInteger(message.BreedingNestStuffId).WriteInteger(message.Result);
    }
}
