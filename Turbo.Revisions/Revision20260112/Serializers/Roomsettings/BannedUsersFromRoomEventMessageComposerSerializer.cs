using Turbo.Primitives.Messages.Outgoing.Roomsettings;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.RoomSettings;

internal class BannedUsersFromRoomEventMessageComposerSerializer(int header)
    : AbstractSerializer<BannedUsersFromRoomEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        BannedUsersFromRoomEventMessageComposer message
    )
    {
        packet.WriteInteger(message.RoomId).WriteInteger(message.BannedUsers.Length);

        foreach (var banned in message.BannedUsers)
        {
            packet.WriteInteger(banned.PlayerId).WriteString(banned.Name);
        }
    }
}
