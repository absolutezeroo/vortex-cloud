using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class HabboClubExtendOfferMessageComposerSerializer(int header)
    : AbstractSerializer<HabboClubExtendOfferMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        HabboClubExtendOfferMessageComposer message
    )
    {
        HabboClubOffersMessageComposerSerializer.SerializeOffer(packet, message.Offer);
        packet.WriteInteger(message.OriginalPricePerMonth);
        packet.WriteInteger(message.OriginalActivityPointPricePerMonth);
        packet.WriteInteger(message.OriginalActivityPointType);
        packet.WriteInteger(message.SubscriptionDaysLeft);
    }
}
