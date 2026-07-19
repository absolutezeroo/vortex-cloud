using Vortex.Primitives.Messages.Outgoing.Room.Session;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Session;

internal class RoomReadyMessageComposerSerializer(int header)
    : AbstractSerializer<RoomReadyMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomReadyMessageComposer message)
    {
        packet.WriteString(message.WorldType).WriteInteger(message.RoomId);
    }
}
