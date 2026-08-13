using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftClaimResultMessageComposerSerializer(int header)
    : AbstractSerializer<NftClaimResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NftClaimResultMessageComposer message
    ) => packet.WriteShort((short)message.Status);
}
