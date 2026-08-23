using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Chat;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Chat;

internal class RoomChatSettingsMessageComposerSerializer(int header)
    : AbstractSerializer<RoomChatSettingsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomChatSettingsMessageComposer message)
    {
        // One int, not the five GuestRoomData writes for the same settings object: this parser
        // calls fromFloodSensitivity, which fills the other four fields with client-side defaults.
        packet.WriteInteger((int)message.FloodSensitivity);
    }
}
