using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class RoomAdPurchaseInfoEventMessageComposerSerializer(int header)
    : AbstractSerializer<RoomAdPurchaseInfoEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RoomAdPurchaseInfoEventMessageComposer message
    )
    {
        packet.WriteBoolean(message.IsVip).WriteInteger(message.Rooms.Length);

        foreach (RoomAdRoomEntry room in message.Rooms)
        {
            packet
                .WriteInteger(room.RoomId)
                .WriteString(room.RoomName)
                .WriteBoolean(room.IsEventRoom);
        }
    }
}
