using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class MiniMailUnreadCountMessageSerializer(int header)
    : AbstractSerializer<MiniMailUnreadCountMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        MiniMailUnreadCountMessageComposer message
    )
    {
        packet.WriteInteger(message.UnreadCount);
    }
}
