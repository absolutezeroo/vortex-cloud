using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog.Data;

internal class CatalogPageSnapshotSerializer
{
    public static void Serialize(IServerPacket packet, CatalogPageSnapshot message)
    {
        packet
            .WriteBoolean(message.Visible)
            .WriteInteger(message.Icon)
            .WriteInteger(message.Id)
            .WriteString(message.Name ?? string.Empty)
            .WriteString(message.Localization ?? string.Empty);
    }
}
