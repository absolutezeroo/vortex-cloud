using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Snapshots.FriendList;

namespace Turbo.Revisions.Revision20260112.Serializers.FriendList;

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
