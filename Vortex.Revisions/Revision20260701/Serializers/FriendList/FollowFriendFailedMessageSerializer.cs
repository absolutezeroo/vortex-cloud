using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.FriendList;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class FollowFriendFailedMessageSerializer(int header)
    : AbstractSerializer<FollowFriendFailedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FollowFriendFailedMessageComposer message
    )
    {
        packet.WriteInteger((int)message.ErrorCode);
    }
}
