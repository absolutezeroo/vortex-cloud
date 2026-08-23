using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftTransferFeeMessageComposerSerializer(int header)
    : AbstractSerializer<NftTransferFeeMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NftTransferFeeMessageComposer message
    ) => packet.WriteInteger(message.Fee);
}
