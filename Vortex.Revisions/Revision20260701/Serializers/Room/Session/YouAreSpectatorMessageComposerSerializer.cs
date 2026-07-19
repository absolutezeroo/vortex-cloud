using Vortex.Primitives.Messages.Outgoing.Room.Session;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Session;

internal class YouAreSpectatorMessageComposerSerializer(int header)
    : AbstractSerializer<YouAreSpectatorMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, YouAreSpectatorMessageComposer message)
    {
        packet.WriteInteger(message.RoomId);
    }
}
