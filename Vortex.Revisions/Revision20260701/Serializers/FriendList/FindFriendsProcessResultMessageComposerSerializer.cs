using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.FriendList;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class FindFriendsProcessResultMessageComposerSerializer(int header)
    : AbstractSerializer<FindFriendsProcessResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FindFriendsProcessResultMessageComposer message
    )
    {
        packet.WriteBoolean(message.Success);
    }
}
