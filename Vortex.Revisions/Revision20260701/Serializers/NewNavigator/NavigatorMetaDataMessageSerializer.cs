using Vortex.Primitives.Messages.Outgoing.NewNavigator;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.NewNavigator.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.NewNavigator;

internal class NavigatorMetaDataMessageSerializer(int header)
    : AbstractSerializer<NavigatorMetaDataMessage>(header)
{
    protected override void Serialize(IServerPacket packet, NavigatorMetaDataMessage message)
    {
        packet.WriteInteger(message.TopLevelContexts.Length);

        foreach (NavigatorTopLevelContextSnapshot context in message.TopLevelContexts)
        {
            packet.WriteString(context.SearchCode);
            packet.WriteInteger(context.QuickLinks.Length);

            foreach (NavigatorQuickLinkSnapshot quickLink in context.QuickLinks)
            {
                NavigatorQuickLinkSnapshotSerializer.Serialize(packet, quickLink);
            }
        }
    }
}
