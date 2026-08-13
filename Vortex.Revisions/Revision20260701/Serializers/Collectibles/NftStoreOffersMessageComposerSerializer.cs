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

            // The product struct in its BASE shape: the client reads this one directly through the
            // base class, whose amount hook is a no-op — so no amount field, unlike the
            // collections list.
            CollectibleSerialization.WriteBaseProductItem(packet, offer.ProductInfo);
        }
    }
}
