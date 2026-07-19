using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftRewardItemClaimResultMessageComposerSerializer(int header)
    : AbstractSerializer<NftRewardItemClaimResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NftRewardItemClaimResultMessageComposer message
    )
    {
        //
    }
}
