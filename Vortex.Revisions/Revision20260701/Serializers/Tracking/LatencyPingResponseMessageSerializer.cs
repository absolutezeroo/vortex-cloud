using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Tracking;

namespace Vortex.Revisions.Revision20260701.Serializers.Tracking;

internal class LatencyPingResponseMessageSerializer(int header)
    : AbstractSerializer<LatencyPingResponseMessage>(header)
{
    protected override void Serialize(IServerPacket packet, LatencyPingResponseMessage message)
    {
        packet.WriteInteger(message.RequestId);
    }
}
