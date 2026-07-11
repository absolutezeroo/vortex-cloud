using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.Users;

internal class GuildMembershipsMessageComposerSerializer(int header)
    : AbstractSerializer<GuildMembershipsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, GuildMembershipsMessageComposer message)
    {
        packet.WriteInteger(message.Memberships.Count);
        foreach (GuildInfoSnapshot guild in message.Memberships)
        {
            packet.WriteInteger(guild.GroupId);
            packet.WriteString(guild.GroupName);
            packet.WriteString(guild.BadgeCode);
            packet.WriteString(guild.PrimaryColor);
            packet.WriteString(guild.SecondaryColor);
            packet.WriteBoolean(guild.Favourite);
            packet.WriteInteger(guild.OwnerId);
            packet.WriteBoolean(guild.HasForum);
        }
    }
}
