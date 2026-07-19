using Vortex.Primitives.Messages.Outgoing.NewNavigator;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Snapshots.NewNavigator;

namespace Vortex.Revisions.Revision20260701.Serializers.NewNavigator;

internal class NavigatorLiftedRoomsMessageSerializer(int header)
    : AbstractSerializer<NavigatorLiftedRoomsMessage>(header)
{
    protected override void Serialize(IServerPacket packet, NavigatorLiftedRoomsMessage message)
    {
        packet.WriteInteger(message.LiftedRooms.Count);

        foreach (NavigatorLiftedRoomSnapshot room in message.LiftedRooms)
        {
            packet.WriteInteger(room.FlatId);
            packet.WriteInteger(room.AreaId);
            packet.WriteString(room.Image);
            packet.WriteString(room.Caption);
        }
    }
}
