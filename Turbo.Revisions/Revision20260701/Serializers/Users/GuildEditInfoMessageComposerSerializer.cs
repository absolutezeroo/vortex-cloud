using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.Users;

internal class GuildEditInfoMessageComposerSerializer(int header)
    : AbstractSerializer<GuildEditInfoMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, GuildEditInfoMessageComposer message)
    {
        GroupEditInfoSnapshot info = message.Info;

        packet.WriteInteger(info.OwnedRooms.Count);
        foreach (GroupRoomSnapshot room in info.OwnedRooms)
        {
            packet.WriteInteger(room.RoomId);
            packet.WriteString(room.RoomName);
            packet.WriteBoolean(room.HasControllers);
        }

        packet.WriteBoolean(info.IsOwner);
        packet.WriteInteger(info.GroupId);
        packet.WriteString(info.GroupName);
        packet.WriteString(info.GroupDescription);
        packet.WriteInteger(info.BaseRoomId);
        packet.WriteInteger(info.PrimaryColorId);
        packet.WriteInteger(info.SecondaryColorId);
        packet.WriteInteger(info.GuildType);
        packet.WriteInteger(info.GuildRightsLevel);
        packet.WriteBoolean(info.Locked);
        packet.WriteString(info.Url);

        packet.WriteInteger(info.BadgeParts.Count);
        foreach (GroupBadgePartSnapshot part in info.BadgeParts)
        {
            packet.WriteInteger(part.PartId);
            packet.WriteInteger(part.ColorId);
            packet.WriteInteger(part.Position);
        }

        packet.WriteString(info.BadgeCode);
        packet.WriteInteger(info.MembershipCount);
    }
}
