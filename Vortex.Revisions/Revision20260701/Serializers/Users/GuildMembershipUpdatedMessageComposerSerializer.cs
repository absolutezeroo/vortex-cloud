using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class GuildMembershipUpdatedMessageComposerSerializer(int header)
    : AbstractSerializer<GuildMembershipUpdatedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuildMembershipUpdatedMessageComposer message
    )
    {
        packet.WriteInteger(message.GroupId);

        GroupMemberSnapshot member = message.Member;
        packet.WriteInteger(member.RoleType);
        packet.WriteInteger(member.UserId);
        packet.WriteString(member.UserName);
        packet.WriteString(member.Figure);
        packet.WriteString(member.MemberSince);
    }
}
