using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog.Data;

internal class CatalogFrontPageItemSerializer
{
    public static void Serialize(IServerPacket packet, CatalogFrontPageItemSnapshot message)
    {
        packet
            .WriteInteger(message.Position)
            .WriteString(message.ItemName)
            .WriteString(message.ItemPromoImage)
            .WriteInteger((int)message.Type);

        switch (message.Type)
        {
            case CatalogFrontPageItemType.Default:
                packet.WriteString(message.CatalogPageLocation ?? string.Empty);
                break;
            case CatalogFrontPageItemType.One:
                packet.WriteInteger(message.ProductOfferId ?? 0);
                break;
            case CatalogFrontPageItemType.Two:
                packet.WriteString(message.ProductCode ?? string.Empty);
                break;
        }

        packet.WriteInteger(message.ExpiresInSeconds);
    }
}
