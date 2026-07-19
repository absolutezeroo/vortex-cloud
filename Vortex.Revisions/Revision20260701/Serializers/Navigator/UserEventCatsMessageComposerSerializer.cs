using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class UserEventCatsMessageComposerSerializer(int header)
    : AbstractSerializer<UserEventCatsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UserEventCatsMessageComposer message)
    {
        packet.WriteInteger(message.EventCategories.Length);

        foreach (NavigatorEventCategorySnapshot category in message.EventCategories)
        {
            packet.WriteInteger(category.Id).WriteString(category.Name);
        }
    }
}
