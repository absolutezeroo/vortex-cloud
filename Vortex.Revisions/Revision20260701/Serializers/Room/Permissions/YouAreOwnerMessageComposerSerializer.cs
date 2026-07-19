using Vortex.Primitives.Messages.Outgoing.Room.Permissions;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Permissions;

internal class YouAreOwnerMessageComposerSerializer(int header)
    : AbstractSerializer<YouAreOwnerMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, YouAreOwnerMessageComposer message)
    {
        packet.WriteInteger(message.RoomId);
    }
}
