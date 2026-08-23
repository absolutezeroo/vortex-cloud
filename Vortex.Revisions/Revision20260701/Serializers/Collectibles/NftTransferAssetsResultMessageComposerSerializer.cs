using Vortex.Protocol.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftTransferAssetsResultMessageComposerSerializer(int header)
    : AbstractSerializer<NftTransferAssetsResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NftTransferAssetsResultMessageComposer message
    ) =>
        // Writing nothing here was not a no-op: the client reads success as resultCode == 0, and an
        // absent short reads as zero, so an empty body announced a transfer that never happened.
        packet.WriteShort(message.ResultCode);
}
