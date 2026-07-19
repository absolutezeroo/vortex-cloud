using Vortex.Primitives.Packets;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList.Snapshots;

internal class FriendCategorySnapshotSerializer
{
    public static void Serialize(IServerPacket packet, FriendCategorySnapshot message)
    {
        packet.WriteInteger(message.Id);
        packet.WriteString(message.Name);
    }
}
