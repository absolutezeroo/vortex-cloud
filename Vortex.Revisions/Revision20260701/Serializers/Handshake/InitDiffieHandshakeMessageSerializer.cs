using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Handshake;

namespace Vortex.Revisions.Revision20260701.Serializers.Handshake;

internal class InitDiffieHandshakeMessageSerializer(int header)
    : AbstractSerializer<InitDiffieHandshakeMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        InitDiffieHandshakeMessageComposer composer
    ) => packet.WriteString(composer.Prime).WriteString(composer.Generator);
}
