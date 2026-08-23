using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.FriendList;

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
