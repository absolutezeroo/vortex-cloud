using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Snapshots.FriendList;
using Vortex.Revisions.Revision20260701.Serializers.FriendList.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class HabboSearchResultMessageSerializer(int header)
    : AbstractSerializer<HabboSearchResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        HabboSearchResultMessageComposer message
    )
    {
        packet.WriteInteger(message.Friends.Count);

        foreach (MessengerSearchResultSnapshot friend in message.Friends)
        {
            MessengerSearchResultSnapshotSerializer.Serialize(packet, friend);
        }

        packet.WriteInteger(message.Others.Count);

        foreach (MessengerSearchResultSnapshot other in message.Others)
        {
            MessengerSearchResultSnapshotSerializer.Serialize(packet, other);
        }
    }
}
