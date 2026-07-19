using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Catalog.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class PurchaseOKMessageComposerSerializer(int header)
    : AbstractSerializer<PurchaseOKMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PurchaseOKMessageComposer message)
    {
        CatalogOfferSerializer.SerializeAsPurchased(packet, message.Offer);
    }
}
