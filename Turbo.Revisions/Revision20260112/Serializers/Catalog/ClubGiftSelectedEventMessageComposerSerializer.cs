using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260112.Serializers.Catalog.Data;

namespace Turbo.Revisions.Revision20260112.Serializers.Catalog;

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
