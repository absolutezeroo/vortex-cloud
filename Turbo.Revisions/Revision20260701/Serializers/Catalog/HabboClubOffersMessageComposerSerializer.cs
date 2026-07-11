using Turbo.Primitives.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.Catalog;

internal class HabboClubOffersMessageComposerSerializer(int header)
    : AbstractSerializer<HabboClubOffersMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, HabboClubOffersMessageComposer message)
    {
        packet.WriteInteger(message.Offers.Count);
        foreach (ClubOffer offer in message.Offers)
        {
            SerializeOffer(packet, offer);
        }

        packet.WriteInteger((int)message.Source);
    }

    internal static void SerializeOffer(IServerPacket packet, ClubOffer offer)
    {
        packet.WriteInteger(offer.OfferId);
        packet.WriteString(offer.ProductCode);
        packet.WriteBoolean(false);
        packet.WriteInteger(offer.PriceCredits);
        packet.WriteInteger(offer.PriceActivityPoints);
        packet.WriteInteger(offer.PriceActivityPointType);
        packet.WriteBoolean(offer.IsVip);
        packet.WriteInteger(offer.Months);
        packet.WriteInteger(offer.ExtraDays);
        packet.WriteBoolean(offer.IsGiftable);
        packet.WriteInteger(offer.DaysLeftAfterPurchase);
        packet.WriteInteger(offer.Year);
        packet.WriteInteger(offer.Month);
        packet.WriteInteger(offer.Day);
    }
}
