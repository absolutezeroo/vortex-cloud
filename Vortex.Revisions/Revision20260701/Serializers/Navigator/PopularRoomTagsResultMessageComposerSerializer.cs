using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class PopularRoomTagsResultMessageComposerSerializer(int header)
    : AbstractSerializer<PopularRoomTagsResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PopularRoomTagsResultMessageComposer message
    )
    {
        packet.WriteInteger(message.Tags.Length);

        foreach (string tag in message.Tags)
        {
            packet.WriteString(tag);
        }
    }
}
