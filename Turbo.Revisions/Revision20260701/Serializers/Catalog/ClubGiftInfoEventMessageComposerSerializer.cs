using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260701.Serializers.Catalog.Data;

namespace Turbo.Revisions.Revision20260701.Serializers.Catalog;

internal class ClubGiftInfoEventMessageComposerSerializer(int header)
    : AbstractSerializer<ClubGiftInfoEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ClubGiftInfoEventMessageComposer message
    )
    {
        packet
            .WriteInteger(message.DaysUntilNextGift)
            .WriteInteger(message.GiftsAvailable)
            .WriteInteger(message.Offers.Count);

        foreach (CatalogOfferSnapshot offer in message.Offers)
        {
            CatalogOfferSerializer.Serialize(packet, offer);
        }

        packet.WriteInteger(message.GiftData.Count);

        foreach (ClubGiftOfferData gift in message.GiftData)
        {
            packet
                .WriteInteger(gift.OfferId)
                .WriteBoolean(gift.IsVip)
                .WriteInteger(gift.DaysRequired)
                .WriteBoolean(gift.IsSelectable);
        }
    }
}
