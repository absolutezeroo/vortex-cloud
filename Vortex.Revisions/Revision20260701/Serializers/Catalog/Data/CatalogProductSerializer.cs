using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog.Data;

internal class CatalogProductSerializer
{
    public static void Serialize(IServerPacket packet, CatalogProductSnapshot product)
    {
        packet.WriteString(product.ProductType.ToLegacyString());

        if (product.ProductType is not ProductType.Badge)
        {
            packet
                .WriteInteger(product.SpriteId)
                .WriteString(product.ExtraParam ?? string.Empty)
                .WriteInteger(product.Quantity)
                .WriteBoolean(product.UniqueSize > 0);

            if (product.UniqueSize > 0)
            {
                packet.WriteInteger(product.UniqueSize).WriteInteger(product.UniqueRemaining);
            }
        }
        else
        {
            packet.WriteString(product.ExtraParam ?? string.Empty);
        }
    }
}
