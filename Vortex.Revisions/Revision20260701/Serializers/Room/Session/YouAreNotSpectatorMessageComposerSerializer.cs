using Vortex.Primitives.Messages.Outgoing.Room.Session;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Session;

internal class YouAreNotSpectatorMessageComposerSerializer(int header)
    : AbstractSerializer<YouAreNotSpectatorMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        YouAreNotSpectatorMessageComposer message
    )
    {
        packet.WriteInteger(message.RoomId);
    }
}
