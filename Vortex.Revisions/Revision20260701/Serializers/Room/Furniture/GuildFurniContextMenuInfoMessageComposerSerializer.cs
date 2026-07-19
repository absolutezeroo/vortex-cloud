using Vortex.Primitives.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class GuildFurniContextMenuInfoMessageComposerSerializer(int header)
    : AbstractSerializer<GuildFurniContextMenuInfoMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuildFurniContextMenuInfoMessageComposer message
    )
    {
        packet
            .WriteInteger(message.ObjectId)
            .WriteInteger(message.GuildId)
            .WriteString(message.GuildName)
            .WriteInteger(message.GuildHomeRoomId)
            .WriteBoolean(message.UserIsMember)
            .WriteBoolean(message.GuildHasReadableForum);
    }
}
