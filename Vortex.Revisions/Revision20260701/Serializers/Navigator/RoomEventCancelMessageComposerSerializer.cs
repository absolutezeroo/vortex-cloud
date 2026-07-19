using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class RoomEventCancelMessageComposerSerializer(int header)
    : AbstractSerializer<RoomEventCancelMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomEventCancelMessageComposer message)
    {
        packet.WriteInteger(message.RoomId);
    }
}
