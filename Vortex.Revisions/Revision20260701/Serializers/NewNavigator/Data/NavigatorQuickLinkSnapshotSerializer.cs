using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.NewNavigator.Data;

internal class NavigatorQuickLinkSnapshotSerializer
{
    public static void Serialize(IServerPacket packet, NavigatorQuickLinkSnapshot message)
    {
        packet
            .WriteInteger(message.Id)
            .WriteString(message.SearchCode)
            .WriteString(message.Filter)
            .WriteString(message.Localization);
    }
}
