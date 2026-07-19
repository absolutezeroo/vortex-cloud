using Vortex.Primitives.Messages.Outgoing.Marketplace;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Marketplace;

internal class MarketplaceCanMakeOfferResultMessageComposerSerializer(int header)
    : AbstractSerializer<MarketplaceCanMakeOfferResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        MarketplaceCanMakeOfferResultMessageComposer message
    )
    {
        packet.WriteBoolean(message.CanMakeOffer);
    }
}
