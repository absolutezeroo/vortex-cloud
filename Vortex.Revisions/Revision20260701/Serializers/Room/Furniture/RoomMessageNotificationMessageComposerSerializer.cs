using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Furniture;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class RoomMessageNotificationMessageComposerSerializer(int header)
    : AbstractSerializer<RoomMessageNotificationMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RoomMessageNotificationMessageComposer message
    )
    {
        packet
            .WriteInteger(message.RoomId)
            .WriteString(message.RoomName)
            .WriteInteger(message.MessageCount);
    }
}
