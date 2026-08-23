using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Furniture;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class CustomUserNotificationMessageComposerSerializer(int header)
    : AbstractSerializer<CustomUserNotificationMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CustomUserNotificationMessageComposer message
    )
    {
        packet.WriteInteger((int)message.Code);
    }
}
