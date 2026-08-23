using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Notifications;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class HabboBroadcastMessageComposerSerializer(int header)
    : AbstractSerializer<HabboBroadcastMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, HabboBroadcastMessageComposer message)
    {
        packet.WriteString(message.MessageText);
    }
}
