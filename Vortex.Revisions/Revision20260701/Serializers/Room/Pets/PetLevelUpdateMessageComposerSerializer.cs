using Vortex.Protocol.Messages.Outgoing.Room.Pets;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Pets;

internal class PetLevelUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<PetLevelUpdateMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PetLevelUpdateMessageComposer message)
    {
        packet
            .WriteInteger(message.RoomIndex)
            .WriteInteger(message.PetId)
            .WriteInteger(message.Level);
    }
}
