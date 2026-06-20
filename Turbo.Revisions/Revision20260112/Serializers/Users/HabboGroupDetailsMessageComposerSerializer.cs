using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Users;

internal class HabboGroupDetailsMessageComposerSerializer(int header)
    : AbstractSerializer<HabboGroupDetailsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        HabboGroupDetailsMessageComposer message
    )
    {
        GroupDetailsSnapshot details = message.Details;

        packet.WriteInteger(details.GroupId);
        packet.WriteBoolean(details.IsGuild);
        packet.WriteInteger(details.Type);
        packet.WriteString(details.Name);
        packet.WriteString(details.Description);
        packet.WriteString(details.BadgeCode);
        packet.WriteInteger(details.RoomId);
        packet.WriteString(details.RoomName);
        packet.WriteInteger(details.Status);
        packet.WriteInteger(details.TotalMembers);
        packet.WriteBoolean(details.Favourite);
        packet.WriteString(details.CreationDate);
        packet.WriteBoolean(details.IsOwner);
        packet.WriteBoolean(details.IsAdmin);
        packet.WriteString(details.OwnerName);
        packet.WriteBoolean(details.OpenToJoin);
        packet.WriteBoolean(details.MembersCanDecorate);
        packet.WriteInteger(details.PendingMemberCount);
        packet.WriteBoolean(details.HasForum);
    }
}
