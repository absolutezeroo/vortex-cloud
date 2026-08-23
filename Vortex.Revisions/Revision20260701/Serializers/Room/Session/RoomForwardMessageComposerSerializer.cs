using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Session;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Session;

internal class RoomForwardMessageComposerSerializer(int header)
    : AbstractSerializer<RoomForwardMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomForwardMessageComposer message)
    {
        packet.WriteInteger(message.RoomId);
    }
}
