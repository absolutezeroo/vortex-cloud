using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Navigator;
using Vortex.Revisions.Revision20260701.Serializers.Navigator.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class GuestRoomSearchResultMessageComposerSerializer(int header)
    : AbstractSerializer<GuestRoomSearchResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuestRoomSearchResultMessageComposer message
    )
    {
        packet
            .WriteInteger(message.SearchType)
            .WriteString(message.SearchParam)
            .WriteInteger(message.Rooms.Length);

        foreach (RoomInfoSnapshot room in message.Rooms)
        {
            RoomSettingsSerializer.Serialize(packet, room);
        }

        // A boolean guard here, where the sibling OfficialRooms message uses an int for the same
        // "is there an ad entry" question. The two are not interchangeable.
        if (message.Ad is null)
        {
            packet.WriteBoolean(false);

            return;
        }

        packet.WriteBoolean(true);

        OfficialRoomsMessageComposerSerializer.SerializeEntry(packet, message.Ad);
    }
}
