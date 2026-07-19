using Vortex.Primitives.Messages.Outgoing.Room.Session;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Session;

internal class OpenConnectionMessageComposerSerializer(int header)
    : AbstractSerializer<OpenConnectionMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, OpenConnectionMessageComposer message)
    {
        packet.WriteInteger(message.RoomId);
    }
}
