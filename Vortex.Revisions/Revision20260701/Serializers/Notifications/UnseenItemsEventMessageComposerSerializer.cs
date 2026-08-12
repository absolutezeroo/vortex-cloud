using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class UnseenItemsEventMessageComposerSerializer(int header)
    : AbstractSerializer<UnseenItemsEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UnseenItemsEventMessageComposer message)
    {
        packet.WriteInteger(message.Categories.Length);

        foreach (UnseenItemCategory category in message.Categories)
        {
            packet.WriteInteger(category.CategoryId).WriteInteger(category.ItemIds.Length);

            foreach (int itemId in category.ItemIds)
            {
                packet.WriteInteger(itemId);
            }
        }
    }
}
