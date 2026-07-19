using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class FriendNotificationMessageSerializer(int header)
    : AbstractSerializer<FriendNotificationMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FriendNotificationMessageComposer message
    )
    {
        packet.WriteString(message.AvatarId);
        packet.WriteInteger((int)message.TypeCode);
        packet.WriteString(message.Message);
    }
}
