using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class CanCreateRoomMessageComposerSerializer(int header)
    : AbstractSerializer<CanCreateRoomMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CanCreateRoomMessageComposer message)
    {
        packet.WriteInteger(message.ResultCode).WriteInteger(message.RoomLimit);
    }
}
