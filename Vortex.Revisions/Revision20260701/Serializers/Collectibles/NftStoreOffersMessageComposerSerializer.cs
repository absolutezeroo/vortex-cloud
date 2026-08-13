using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Collectibles.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftStoreOffersMessageComposerSerializer(int header)
    : AbstractSerializer<NftStoreOffersMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, NftStoreOffersMessageComposer message)
    {
        packet.WriteInteger(message.Offers.Length);

        foreach (NftStoreOfferSnapshot offer in message.Offers)
        {
            packet
                .WriteString(offer.ProductCode)
                .WriteInteger(offer.EmeraldPrice)
                .WriteBoolean(offer.IsFeatured)
                .WriteBoolean(offer.IsLimited)
                .WriteInteger(offer.MintLimit)
                .WriteInteger(offer.MintedCount);

            // The same product struct the collections list carries, written by the one helper that
            // knows the amount lands partway down it rather than at the end.
            CollectibleSerialization.WriteProductItem(packet, offer.ProductInfo);
        }
    }
}
