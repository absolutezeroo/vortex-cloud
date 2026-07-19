using Vortex.Primitives.Messages.Outgoing.Tracking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Tracking;

internal class LatencyPingResponseMessageSerializer(int header)
    : AbstractSerializer<LatencyPingResponseMessage>(header)
{
    protected override void Serialize(IServerPacket packet, LatencyPingResponseMessage message)
    {
        packet.WriteInteger(message.RequestId);
    }
}
