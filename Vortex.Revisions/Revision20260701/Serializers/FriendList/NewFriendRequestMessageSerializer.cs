using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.FriendList.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class NewFriendRequestMessageSerializer(int header)
    : AbstractSerializer<NewFriendRequestMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, NewFriendRequestMessageComposer message)
    {
        FriendRequestSnapshotSerializer.Serialize(packet, message.Request);
    }
}
