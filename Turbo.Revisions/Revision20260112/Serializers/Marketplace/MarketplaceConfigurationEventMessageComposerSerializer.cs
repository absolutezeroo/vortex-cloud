using Turbo.Primitives.Messages.Outgoing.Marketplace;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Marketplace;

internal class MarketplaceConfigurationEventMessageComposerSerializer(int header)
    : AbstractSerializer<MarketplaceConfigurationEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        MarketplaceConfigurationEventMessageComposer message
    )
    {
        packet.WriteBoolean(message.Enabled);
        packet.WriteInteger(message.Commission);
        packet.WriteInteger(message.Credits);
        packet.WriteInteger(message.Advertisements);
        packet.WriteInteger(message.MinimumPrice);
        packet.WriteInteger(message.MaximumPrice);
        packet.WriteInteger(message.OfferTime);
        packet.WriteInteger(message.DisplayTime);
        packet.WriteInteger(message.SellingFeePercentage);
        packet.WriteInteger(message.RevenueLimit);
        packet.WriteInteger(message.HalfTaxLimit);
    }
}
