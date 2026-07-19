using Vortex.Primitives.Messages.Outgoing.NewNavigator;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.NewNavigator.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.NewNavigator;

internal class NavigatorSavedSearchesMessageSerializer(int header)
    : AbstractSerializer<NavigatorSavedSearchesMessage>(header)
{
    protected override void Serialize(IServerPacket packet, NavigatorSavedSearchesMessage message)
    {
        packet.WriteInteger(message.SavedSearches.Count);

        foreach (NavigatorQuickLinkSnapshot savedSearch in message.SavedSearches)
        {
            NavigatorQuickLinkSnapshotSerializer.Serialize(packet, savedSearch);
        }
    }
}
