using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;

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
