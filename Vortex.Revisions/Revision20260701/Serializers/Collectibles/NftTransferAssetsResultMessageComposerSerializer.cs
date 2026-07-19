using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftTransferAssetsResultMessageComposerSerializer(int header)
    : AbstractSerializer<NftTransferAssetsResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NftTransferAssetsResultMessageComposer message
    )
    {
        //
    }
}
