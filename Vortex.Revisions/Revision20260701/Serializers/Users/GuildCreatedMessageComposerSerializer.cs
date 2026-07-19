using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class GuildCreatedMessageComposerSerializer(int header)
    : AbstractSerializer<GuildCreatedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, GuildCreatedMessageComposer message)
    {
        packet.WriteInteger(message.BaseRoomId);
        packet.WriteInteger(message.GroupId);
    }
}
