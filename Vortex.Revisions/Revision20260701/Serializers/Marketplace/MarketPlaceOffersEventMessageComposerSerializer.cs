using Vortex.Primitives.Marketplace.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Marketplace;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Marketplace;

internal class MarketPlaceOffersEventMessageComposerSerializer(int header)
    : AbstractSerializer<MarketPlaceOffersEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        MarketPlaceOffersEventMessageComposer message
    )
    {
        packet.WriteInteger(message.Offers.Count);
        foreach (MarketplaceOfferSnapshot offer in message.Offers)
        {
            MarketplaceOfferWriter.WriteOffer(packet, offer, true);
        }

        packet.WriteInteger(message.TotalFound);
    }
}
