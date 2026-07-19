using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Snapshots.FriendList;
using Vortex.Revisions.Revision20260701.Serializers.FriendList.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class MessengerInitMessageSerializer(int header)
    : AbstractSerializer<MessengerInitMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, MessengerInitMessageComposer message)
    {
        packet.WriteInteger(message.UserFriendLimit);
        packet.WriteInteger(message.NormalFriendLimit);
        packet.WriteInteger(message.ExtendedFriendLimit);
        packet.WriteInteger(message.FriendCategories.Count);

        foreach (FriendCategorySnapshot category in message.FriendCategories)
        {
            FriendCategorySnapshotSerializer.Serialize(packet, category);
        }
    }
}
