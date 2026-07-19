using Vortex.Primitives.Messages.Outgoing.Handshake;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Handshake;

internal class NoobnessLevelMessageSerializer(int header)
    : AbstractSerializer<NoobnessLevelMessage>(header)
{
    protected override void Serialize(IServerPacket packet, NoobnessLevelMessage message)
    {
        packet.WriteInteger((int)message.NoobnessLevel);
    }
}
