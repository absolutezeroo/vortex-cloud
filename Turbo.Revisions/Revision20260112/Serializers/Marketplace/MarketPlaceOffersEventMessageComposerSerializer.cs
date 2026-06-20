using Turbo.Primitives.Marketplace.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Marketplace;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Marketplace;

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
