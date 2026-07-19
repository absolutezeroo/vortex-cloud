using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class RespectNotificationMessageComposerSerializer(int header)
    : AbstractSerializer<RespectNotificationMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RespectNotificationMessageComposer message
    )
    {
        packet.WriteInteger(message.UserId);
        packet.WriteInteger(message.RespectTotal);
    }
}
