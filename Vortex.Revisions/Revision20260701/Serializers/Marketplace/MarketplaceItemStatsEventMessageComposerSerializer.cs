using Vortex.Primitives.Messages.Outgoing.Marketplace;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Marketplace;

internal class MarketplaceItemStatsEventMessageComposerSerializer(int header)
    : AbstractSerializer<MarketplaceItemStatsEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        MarketplaceItemStatsEventMessageComposer message
    )
    {
        packet.WriteInteger(message.AvgPrice);
        packet.WriteInteger(message.OfferCount);
        packet.WriteInteger(0);
        packet.WriteInteger(0);
        packet.WriteInteger(message.CategoryId);
        packet.WriteInteger(message.TypeId);
    }
}
