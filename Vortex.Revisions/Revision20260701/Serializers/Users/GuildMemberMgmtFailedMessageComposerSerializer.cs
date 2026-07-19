using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class GuildMemberMgmtFailedMessageComposerSerializer(int header)
    : AbstractSerializer<GuildMemberMgmtFailedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuildMemberMgmtFailedMessageComposer message
    )
    {
        packet.WriteInteger(message.GroupId);
        packet.WriteInteger(message.Reason);
    }
}
