using Vortex.Primitives.Messages.Outgoing.Room.Pets;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Pets.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Pets;

internal class PetStatusUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<PetStatusUpdateMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PetStatusUpdateMessageComposer message)
    {
        packet
            .WriteInteger(message.RoomIndex)
            .WriteInteger(message.PetId)
            .WriteBoolean(message.CanBreed)
            .WriteBoolean(message.CanHarvest)
            .WriteBoolean(message.CanRevive)
            .WriteBoolean(message.HasBreedingPermission);
    }
}
