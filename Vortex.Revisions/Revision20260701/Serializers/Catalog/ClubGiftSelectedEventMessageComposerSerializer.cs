using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Catalog.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class ClubGiftSelectedEventMessageComposerSerializer(int header)
    : AbstractSerializer<ClubGiftSelectedEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ClubGiftSelectedEventMessageComposer message
    )
    {
        packet.WriteString(message.ProductCode).WriteInteger(message.Products.Count);

        foreach (CatalogProductSnapshot product in message.Products)
        {
            CatalogProductSerializer.Serialize(packet, product);
        }
    }
}
