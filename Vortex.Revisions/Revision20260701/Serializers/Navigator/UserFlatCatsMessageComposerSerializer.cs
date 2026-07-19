using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class UserFlatCatsMessageComposerSerializer(int header)
    : AbstractSerializer<UserFlatCatsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UserFlatCatsMessageComposer message)
    {
        packet.WriteInteger(message.Categories.Length);

        foreach (NavigatorFlatCategorySnapshot cat in message.Categories)
        {
            packet.WriteInteger(cat.Id);
            packet.WriteString(cat.Name);
            packet.WriteBoolean(cat.Visible);
            packet.WriteBoolean(cat.Automatic);
            packet.WriteString(cat.AutomaticCategory);
            packet.WriteString(cat.GlobalCategory);
            packet.WriteBoolean(cat.StaffOnly);
        }
    }
}
