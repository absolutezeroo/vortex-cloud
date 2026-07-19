using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog.Data;

internal class CatalogPageLocalizationSerializer
{
    public static void Serialize(IServerPacket packet, CatalogPageSnapshot message)
    {
        packet.WriteInteger(message.ImageData.Count);

        foreach (string data in message.ImageData)
        {
            packet.WriteString(data);
        }

        packet.WriteInteger(message.TextData.Count);

        foreach (string data in message.TextData)
        {
            packet.WriteString(data);
        }
    }
}
