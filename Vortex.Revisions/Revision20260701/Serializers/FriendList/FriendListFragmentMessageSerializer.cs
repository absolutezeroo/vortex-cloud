using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Snapshots.FriendList;
using Vortex.Revisions.Revision20260701.Serializers.FriendList.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class FriendListFragmentMessageSerializer(int header)
    : AbstractSerializer<FriendListFragmentMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FriendListFragmentMessageComposer message
    )
    {
        packet.WriteInteger(message.TotalFragments);
        packet.WriteInteger(message.FragmentIndex);
        packet.WriteInteger(message.Fragment.Count);

        foreach (MessengerFriendSnapshot friend in message.Fragment)
        {
            MessengerFriendSnapshotSerializer.Serialize(packet, friend);
        }
    }
}
