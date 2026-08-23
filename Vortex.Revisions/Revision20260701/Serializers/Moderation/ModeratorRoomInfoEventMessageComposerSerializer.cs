using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Moderation;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class ModeratorRoomInfoEventMessageComposerSerializer(int header)
    : AbstractSerializer<ModeratorRoomInfoEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ModeratorRoomInfoEventMessageComposer message
    )
    {
        packet
            .WriteInteger(message.RoomId)
            .WriteInteger(message.UserCount)
            .WriteBoolean(message.OwnerInRoom)
            .WriteInteger(message.OwnerId)
            .WriteString(message.OwnerName)
            .WriteBoolean(message.RoomExists);

        // The client's room block stops reading right after the flag when the room is gone, so
        // writing name/desc/tags anyway would leave unread bytes behind in the buffer.
        if (!message.RoomExists)
        {
            return;
        }

        packet
            .WriteString(message.RoomName)
            .WriteString(message.RoomDescription)
            .WriteInteger(message.Tags.Length);

        foreach (string tag in message.Tags)
        {
            packet.WriteString(tag);
        }
    }
}
