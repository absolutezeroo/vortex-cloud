using Vortex.Primitives.Messages.Outgoing.Room.Session;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Session;

internal class FlatAccessibleMessageComposerSerializer(int header)
    : AbstractSerializer<FlatAccessibleMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, FlatAccessibleMessageComposer message)
    {
        packet.WriteInteger(message.RoomId).WriteString(message.Username);
    }
}
