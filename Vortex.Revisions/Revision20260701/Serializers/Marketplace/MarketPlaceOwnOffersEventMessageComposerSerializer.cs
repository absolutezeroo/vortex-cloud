using Vortex.Primitives.Marketplace.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Marketplace;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Marketplace;

internal class MarketPlaceOwnOffersEventMessageComposerSerializer(int header)
    : AbstractSerializer<MarketPlaceOwnOffersEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        MarketPlaceOwnOffersEventMessageComposer message
    )
    {
        packet.WriteInteger(message.CreditsWaiting);
        packet.WriteInteger(message.Offers.Count);
        foreach (MarketplaceOfferSnapshot offer in message.Offers)
        {
            MarketplaceOfferWriter.WriteOffer(packet, offer, false);
        }
    }
}
