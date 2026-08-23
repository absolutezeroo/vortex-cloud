using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Permissions;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Permissions;

internal class YouAreControllerMessageComposerSerializer(int header)
    : AbstractSerializer<YouAreControllerMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, YouAreControllerMessageComposer message)
    {
        packet.WriteInteger(message.RoomId).WriteInteger((int)message.ControllerLevel);
    }
}
