using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Notifications;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class HabboActivityPointNotificationMessageComposerSerializer(int header)
    : AbstractSerializer<HabboActivityPointNotificationMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        HabboActivityPointNotificationMessageComposer message
    )
    {
        packet
            .WriteInteger(message.Amount)
            .WriteInteger(message.Change)
            .WriteInteger(message.ActivityPointType);
    }
}
