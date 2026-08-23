using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Session;

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
