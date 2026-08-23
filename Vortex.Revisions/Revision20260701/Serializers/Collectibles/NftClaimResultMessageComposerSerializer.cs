using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftClaimResultMessageComposerSerializer(int header)
    : AbstractSerializer<NftClaimResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NftClaimResultMessageComposer message
    ) => packet.WriteShort((short)message.Status);
}
