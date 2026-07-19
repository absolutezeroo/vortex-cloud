using Vortex.Primitives.Messages.Outgoing.Marketplace;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Marketplace;

internal class MarketplaceBuyOfferResultEventMessageComposerSerializer(int header)
    : AbstractSerializer<MarketplaceBuyOfferResultEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        MarketplaceBuyOfferResultEventMessageComposer message
    )
    {
        packet.WriteInteger(message.Result);
        packet.WriteInteger(message.OfferId);
        packet.WriteInteger(message.NewPrice);
        packet.WriteInteger(message.OldOfferId);
    }
}
