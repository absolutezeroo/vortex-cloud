using Turbo.Primitives.Marketplace.Snapshots;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Marketplace;

internal static class MarketplaceOfferWriter
{
    private const int TYPE_LANDSCAPE = 1;

    internal static void WriteOffer(
        IServerPacket packet,
        MarketplaceOfferSnapshot offer,
        bool includeOfferCount
    )
    {
        packet.WriteInteger(offer.OfferId);
        packet.WriteInteger(offer.Status);
        packet.WriteInteger(offer.FurnitureType);

        if (offer.FurnitureType == TYPE_LANDSCAPE)
        {
            packet.WriteInteger(offer.SpriteId);
            packet.WriteInteger(0);
            packet.WriteString(offer.ExtraData ?? string.Empty);
        }
        else
        {
            packet.WriteInteger(offer.SpriteId);
            packet.WriteString(offer.ExtraData ?? string.Empty);
        }

        packet.WriteInteger(offer.Price);
        packet.WriteInteger(offer.ExpiresIn / 60);
        packet.WriteInteger(offer.AvgPrice);

        if (includeOfferCount)
        {
            packet.WriteInteger(offer.OfferCount);
        }
    }
}
