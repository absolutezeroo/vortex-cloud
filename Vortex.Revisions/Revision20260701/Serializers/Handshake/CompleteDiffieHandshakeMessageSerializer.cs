using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Handshake;

namespace Vortex.Revisions.Revision20260701.Serializers.Handshake;

internal class CompleteDiffieHandshakeMessageSerializer(int header)
    : AbstractSerializer<CompleteDiffieHandshakeMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CompleteDiffieHandshakeMessageComposer message
    )
    {
        packet.WriteString(message.PublicKey);
        packet.WriteBoolean(message.ServerClientEncryption);
    }
}
