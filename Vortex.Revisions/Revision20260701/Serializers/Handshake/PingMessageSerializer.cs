using Vortex.Primitives.Messages.Outgoing.Handshake;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Handshake;

internal class PingMessageSerializer(int header) : AbstractSerializer<PingMessage>(header)
{
    protected override void Serialize(IServerPacket packet, PingMessage message)
    {
        //
    }
}
