using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftTransferFeeMessageComposerSerializer(int header)
    : AbstractSerializer<NftTransferFeeMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, NftTransferFeeMessageComposer message)
    {
        //
    }
}
