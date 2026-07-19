using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class HabboGroupJoinFailedMessageComposerSerializer(int header)
    : AbstractSerializer<HabboGroupJoinFailedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        HabboGroupJoinFailedMessageComposer message
    )
    {
        packet.WriteInteger(message.Reason);
    }
}
