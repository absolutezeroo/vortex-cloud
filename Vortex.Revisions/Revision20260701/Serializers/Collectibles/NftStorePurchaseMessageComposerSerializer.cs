using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftStorePurchaseMessageComposerSerializer(int header)
    : AbstractSerializer<NftStorePurchaseMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NftStorePurchaseMessageComposer message
    ) => packet.WriteShort(message.Result);
}
