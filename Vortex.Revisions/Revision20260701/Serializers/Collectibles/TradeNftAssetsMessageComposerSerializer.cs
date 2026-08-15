using System.Collections.Immutable;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Collectibles.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class TradeNftAssetsMessageComposerSerializer(int header)
    : AbstractSerializer<TradeNftAssetsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, TradeNftAssetsMessageComposer message)
    {
        WriteAssets(packet, message.MyAssets);
        WriteAssets(packet, message.TheirAssets);
    }

    /// <summary>
    /// The same asset struct the inventory's Collectibles tab reads: the id is a long and goes
    /// <em>ahead</em> of the product struct, because the client's class reads its own field before
    /// chaining to the base constructor. The base struct writes no amount — an asset is one item.
    /// </summary>
    private static void WriteAssets(
        IServerPacket packet,
        ImmutableArray<CollectibleAssetSnapshot> assets
    )
    {
        packet.WriteInteger(assets.Length);

        foreach (CollectibleAssetSnapshot asset in assets)
        {
            packet.WriteLong(asset.AssetId);
            CollectibleSerialization.WriteBaseProductItem(packet, asset.Product);
        }
    }
}
