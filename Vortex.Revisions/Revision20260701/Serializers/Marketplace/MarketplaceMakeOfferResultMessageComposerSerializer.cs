using Vortex.Primitives.Messages.Outgoing.Marketplace;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Marketplace;

internal class MarketplaceMakeOfferResultMessageComposerSerializer(int header)
    : AbstractSerializer<MarketplaceMakeOfferResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        MarketplaceMakeOfferResultMessageComposer message
    )
    {
        packet.WriteInteger(message.Result);
    }
}
