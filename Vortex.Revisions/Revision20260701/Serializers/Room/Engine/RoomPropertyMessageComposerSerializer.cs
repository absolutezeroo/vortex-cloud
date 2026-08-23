using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class RoomPropertyMessageComposerSerializer(int header)
    : AbstractSerializer<RoomPropertyMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomPropertyMessageComposer message)
    {
        packet.WriteString(message.Key).WriteString(message.Value);
    }
}
