using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Orleans.Snapshots.Navigator;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Navigator;

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
