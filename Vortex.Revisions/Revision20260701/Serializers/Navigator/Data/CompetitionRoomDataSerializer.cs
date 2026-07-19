using Vortex.Primitives.Packets;
using Vortex.Primitives.Snapshots.Navigator;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator.Data;

internal class CompetitionRoomDataSerializer
{
    public static void Serialize(IServerPacket packet, CompetitionRoomDataSnapshot message)
    {
        packet.WriteInteger(message.GoalId);
        packet.WriteInteger(message.PageIndex);
        packet.WriteInteger(message.PageCount);
    }
}
