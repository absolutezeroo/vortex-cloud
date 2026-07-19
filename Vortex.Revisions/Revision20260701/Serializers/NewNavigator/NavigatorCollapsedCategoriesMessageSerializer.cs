using Vortex.Primitives.Messages.Outgoing.NewNavigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.NewNavigator;

internal class NavigatorCollapsedCategoriesMessageSerializer(int header)
    : AbstractSerializer<NavigatorCollapsedCategoriesMessage>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NavigatorCollapsedCategoriesMessage message
    )
    {
        packet.WriteInteger(message.CollapsedCategoryIds.Count);

        foreach (string categoryId in message.CollapsedCategoryIds)
        {
            packet.WriteString(categoryId);
        }
    }
}
