using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class InfoFeedEnableMessageComposerSerializer(int header)
    : AbstractSerializer<InfoFeedEnableMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, InfoFeedEnableMessageComposer message)
    {
        packet.WriteBoolean(message.Enabled);
    }
}
