using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Navigator.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class GetGuestRoomResultMessageComposerSerializer(int header)
    : AbstractSerializer<GetGuestRoomResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GetGuestRoomResultMessageComposer message
    )
    {
        packet.WriteBoolean(message.EnterRoom);

        RoomSettingsSerializer.Serialize(packet, message.RoomInfo);

        packet
            .WriteBoolean(message.RoomForward)
            .WriteBoolean(message.StaffPick)
            .WriteBoolean(message.IsGroupMember)
            .WriteBoolean(message.AllInRoomMuted);

        ModSettingsSnapshotSerializer.Serialize(packet, message.RoomInfo.ModSettings);

        packet.WriteBoolean(message.CanMute);

        RoomChatSettingsSerializer.Serialize(packet, message.RoomInfo.ChatSettings);

        packet.WriteBoolean(message.OpeningConnection);
    }
}
