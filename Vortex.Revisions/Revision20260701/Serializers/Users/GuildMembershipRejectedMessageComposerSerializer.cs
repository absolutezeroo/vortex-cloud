using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class GuildMembershipRejectedMessageComposerSerializer(int header)
    : AbstractSerializer<GuildMembershipRejectedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuildMembershipRejectedMessageComposer message
    )
    {
        packet.WriteInteger(message.GroupId);
        packet.WriteInteger(message.UserId);
    }
}
