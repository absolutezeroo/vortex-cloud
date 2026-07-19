using Vortex.Primitives.Messages.Outgoing.Marketplace;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Marketplace;

internal class MarketplaceCancelOfferResultEventMessageComposerSerializer(int header)
    : AbstractSerializer<MarketplaceCancelOfferResultEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        MarketplaceCancelOfferResultEventMessageComposer message
    )
    {
        packet.WriteInteger(message.OfferId);
        packet.WriteBoolean(message.Success);
    }
}
