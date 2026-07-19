using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class RoomInfoUpdatedMessageComposerSerializer(int header)
    : AbstractSerializer<RoomInfoUpdatedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomInfoUpdatedMessageComposer message)
    {
        packet.WriteInteger(message.RoomId);
    }
}
