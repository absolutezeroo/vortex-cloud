using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.Users;

internal class GuildMembersMessageComposerSerializer(int header)
    : AbstractSerializer<GuildMembersMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, GuildMembersMessageComposer message)
    {
        GroupMembersPageSnapshot page = message.Page;

        packet.WriteInteger(page.GroupId);
        packet.WriteString(page.GroupName);
        packet.WriteInteger(page.BaseRoomId);
        packet.WriteString(page.BadgeCode);
        packet.WriteInteger(page.TotalEntries);

        packet.WriteInteger(page.Members.Count);
        foreach (GroupMemberSnapshot member in page.Members)
        {
            packet.WriteInteger(member.RoleType);
            packet.WriteInteger(member.UserId);
            packet.WriteString(member.UserName);
            packet.WriteString(member.Figure);
            packet.WriteString(member.MemberSince);
        }

        packet.WriteBoolean(page.AllowedToManage);
        packet.WriteInteger(page.PageSize);
        packet.WriteInteger(page.PageIndex);
        packet.WriteInteger(page.SearchType);
        packet.WriteString(page.UserNameFilter);
    }
}
