using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class AcceptFriendResultMessageSerializer(int header)
    : AbstractSerializer<AcceptFriendResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AcceptFriendResultMessageComposer message
    )
    {
        packet.WriteInteger(message.Failures.Count);

        foreach (AcceptFriendFailureSnapshot failure in message.Failures)
        {
            packet.WriteInteger(failure.SenderId);
            packet.WriteInteger((int)failure.ErrorCode);
        }
    }
}
