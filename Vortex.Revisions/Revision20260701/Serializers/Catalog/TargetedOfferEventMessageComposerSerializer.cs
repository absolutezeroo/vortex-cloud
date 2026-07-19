using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class TargetedOfferEventMessageComposerSerializer(int header)
    : AbstractSerializer<TargetedOfferEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        TargetedOfferEventMessageComposer message
    )
    {
        TargetedOfferSnapshot offer = message.Offer;

        packet.WriteInteger(offer.TrackingState);
        packet.WriteInteger(offer.Id);
        packet.WriteString(offer.Identifier);
        packet.WriteString(offer.ProductCode);
        packet.WriteInteger(offer.PriceInCredits);
        packet.WriteInteger(offer.PriceInActivityPoints);
        packet.WriteInteger(offer.ActivityPointType);
        packet.WriteInteger(offer.PurchaseLimit);
        packet.WriteInteger(offer.ExpirationSeconds);
        packet.WriteString(offer.Title);
        packet.WriteString(offer.Description);
        packet.WriteString(offer.ImageUrl);
        packet.WriteString(offer.IconImageUrl);
        packet.WriteInteger(offer.OfferType);

        packet.WriteInteger(offer.SubProductCodes.Length);
        foreach (string subProductCode in offer.SubProductCodes)
        {
            packet.WriteString(subProductCode);
        }
    }
}
