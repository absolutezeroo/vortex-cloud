using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftCollectionsScoreMessageComposerSerializer(int header)
    : AbstractSerializer<NftCollectionsScoreMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NftCollectionsScoreMessageComposer message
    )
    {
        //
    }
}
