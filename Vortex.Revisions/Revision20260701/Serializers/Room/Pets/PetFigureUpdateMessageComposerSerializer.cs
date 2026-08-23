using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Pets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Pets.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Pets;

internal class PetFigureUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<PetFigureUpdateMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PetFigureUpdateMessageComposer message)
    {
        packet.WriteInteger(message.RoomIndex).WriteInteger(message.PetId);

        PetFigureDataSerializer.Serialize(packet, message.Pet);

        packet.WriteBoolean(message.HasSaddle).WriteBoolean(message.IsRiding);
    }
}
