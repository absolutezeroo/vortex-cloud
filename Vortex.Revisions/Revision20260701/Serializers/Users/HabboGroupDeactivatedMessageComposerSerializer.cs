using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class HabboGroupDeactivatedMessageComposerSerializer(int header)
    : AbstractSerializer<HabboGroupDeactivatedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        HabboGroupDeactivatedMessageComposer message
    )
    {
        packet.WriteInteger(message.GroupId);
    }
}
