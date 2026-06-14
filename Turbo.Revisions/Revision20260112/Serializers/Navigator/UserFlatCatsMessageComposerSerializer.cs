using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Navigator;

internal class UserFlatCatsMessageComposerSerializer(int header)
    : AbstractSerializer<UserFlatCatsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UserFlatCatsMessageComposer message)
    {
        packet.WriteInteger(message.Categories.Length);

        foreach (var cat in message.Categories)
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
