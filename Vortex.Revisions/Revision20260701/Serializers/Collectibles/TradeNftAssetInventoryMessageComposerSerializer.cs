using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Collectibles;
using Vortex.Revisions.Revision20260701.Serializers.Collectibles.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class TradeNftAssetInventoryMessageComposerSerializer(int header)
    : AbstractSerializer<TradeNftAssetInventoryMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        TradeNftAssetInventoryMessageComposer message
    )
    {
        packet.WriteInteger(message.Assets.Length);

        foreach (CollectibleAssetSnapshot asset in message.Assets)
        {
            // The id goes ahead of the struct, not after it: the client's class reads its own field
            // before chaining to the base constructor.
            packet.WriteLong(asset.AssetId);
            CollectibleSerialization.WriteBaseProductItem(packet, asset.Product);
        }
    }
}
