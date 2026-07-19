using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Snapshots.FriendList;
using Vortex.Revisions.Revision20260701.Serializers.FriendList.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class FriendListUpdateMessageSerializer(int header)
    : AbstractSerializer<FriendListUpdateMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, FriendListUpdateMessageComposer message)
    {
        packet.WriteInteger(message.FriendCategories.Count);

        foreach (FriendCategorySnapshot category in message.FriendCategories)
        {
            FriendCategorySnapshotSerializer.Serialize(packet, category);
        }

        packet.WriteInteger(message.Updates.Count);

        foreach (FriendListUpdateSnapshot update in message.Updates)
        {
            packet.WriteInteger((int)update.ActionType);

            if (update.ActionType is FriendListUpdateActionType.Removed)
            {
                packet.WriteInteger(update.FriendId);

                continue;
            }

            if (update.Friend is null)
            {
                continue;
            }

            MessengerFriendSnapshotSerializer.Serialize(packet, update.Friend);
        }
    }
}
