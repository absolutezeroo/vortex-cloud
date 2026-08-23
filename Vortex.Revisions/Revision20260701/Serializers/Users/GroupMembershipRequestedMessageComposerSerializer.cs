using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Users;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class GroupMembershipRequestedMessageComposerSerializer(int header)
    : AbstractSerializer<GroupMembershipRequestedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GroupMembershipRequestedMessageComposer message
    )
    {
        packet.WriteInteger(message.GroupId);

        GroupMemberSnapshot requester = message.Requester;
        packet.WriteInteger(requester.RoleType);
        packet.WriteInteger(requester.UserId);
        packet.WriteString(requester.UserName);
        packet.WriteString(requester.Figure);
        packet.WriteString(requester.MemberSince);
    }
}
