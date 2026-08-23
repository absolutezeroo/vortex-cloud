using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Permissions;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Permissions;

internal class YouAreOwnerMessageComposerSerializer(int header)
    : AbstractSerializer<YouAreOwnerMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, YouAreOwnerMessageComposer message)
    {
        packet.WriteInteger(message.RoomId);
    }
}
