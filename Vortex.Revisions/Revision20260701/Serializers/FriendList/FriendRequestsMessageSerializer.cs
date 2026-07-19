using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Snapshots.FriendList;
using Vortex.Revisions.Revision20260701.Serializers.FriendList.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class FriendRequestsMessageSerializer(int header)
    : AbstractSerializer<FriendRequestsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, FriendRequestsMessageComposer message)
    {
        int totalRequests = message.Requests.Count;

        packet.WriteInteger(totalRequests);
        packet.WriteInteger(totalRequests);

        foreach (FriendRequestSnapshot request in message.Requests)
        {
            FriendRequestSnapshotSerializer.Serialize(packet, request);
        }
    }
}
