using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class GuildCreationInfoMessageComposerSerializer(int header)
    : AbstractSerializer<GuildCreationInfoMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuildCreationInfoMessageComposer message
    )
    {
        GroupCreationInfoSnapshot info = message.Info;

        packet.WriteInteger(info.CostInCredits);

        packet.WriteInteger(info.Rooms.Count);
        foreach (GroupRoomSnapshot room in info.Rooms)
        {
            packet.WriteInteger(room.RoomId);
            packet.WriteString(room.RoomName);
            packet.WriteBoolean(room.HasControllers);
        }

        packet.WriteInteger(info.BadgeParts.Count);
        foreach (GroupBadgePartSnapshot part in info.BadgeParts)
        {
            packet.WriteInteger(part.PartId);
            packet.WriteInteger(part.ColorId);
            packet.WriteInteger(part.Position);
        }
    }
}
