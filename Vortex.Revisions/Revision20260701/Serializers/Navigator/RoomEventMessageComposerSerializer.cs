using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class RoomEventMessageComposerSerializer(int header)
    : AbstractSerializer<RoomEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomEventMessageComposer message)
    {
        packet
            .WriteInteger(message.RoomId)
            .WriteString(message.Name)
            .WriteString(message.Description ?? string.Empty)
            .WriteInteger(message.CategoryId)
            .WriteInteger(message.MinutesRemaining);
    }
}
