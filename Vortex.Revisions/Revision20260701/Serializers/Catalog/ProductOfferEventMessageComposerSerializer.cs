using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Catalog.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class ProductOfferEventMessageComposerSerializer(int header)
    : AbstractSerializer<ProductOfferEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ProductOfferEventMessageComposer message
    )
    {
        CatalogOfferSerializer.Serialize(packet, message.Offer);
    }
}
