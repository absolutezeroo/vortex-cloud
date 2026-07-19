using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Navigator.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class CompetitionRoomsDataMessageComposerSerializer(int header)
    : AbstractSerializer<CompetitionRoomsDataMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CompetitionRoomsDataMessageComposer message
    )
    {
        CompetitionRoomDataSerializer.Serialize(packet, message.RoomData);
    }
}
