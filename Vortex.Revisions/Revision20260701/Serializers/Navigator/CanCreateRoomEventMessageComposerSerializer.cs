using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class CanCreateRoomEventMessageComposerSerializer(int header)
    : AbstractSerializer<CanCreateRoomEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CanCreateRoomEventMessageComposer message
    )
    {
        packet.WriteBoolean(message.CanCreateEvent).WriteInteger(message.ErrorCode);
    }
}
