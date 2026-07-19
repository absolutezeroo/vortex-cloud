using Vortex.Primitives.Messages.Outgoing.Roomsettings;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

internal class BannedUsersFromRoomEventMessageComposerSerializer(int header)
    : AbstractSerializer<BannedUsersFromRoomEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        BannedUsersFromRoomEventMessageComposer message
    )
    {
        packet.WriteInteger(message.RoomId).WriteInteger(message.BannedUsers.Length);

        foreach (RoomControllerSnapshot banned in message.BannedUsers)
        {
            packet.WriteInteger(banned.PlayerId).WriteString(banned.Name);
        }
    }
}
