using Turbo.Primitives.Marketplace.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Marketplace;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Marketplace;

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
            MarketplaceOfferWriter.WriteOffer(packet, offer, includeOfferCount: false);
    }
}
