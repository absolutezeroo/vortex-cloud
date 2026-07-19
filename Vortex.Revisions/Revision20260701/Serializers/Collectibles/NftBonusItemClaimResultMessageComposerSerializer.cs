using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftBonusItemClaimResultMessageComposerSerializer(int header)
    : AbstractSerializer<NftBonusItemClaimResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NftBonusItemClaimResultMessageComposer message
    )
    {
        //
    }
}
