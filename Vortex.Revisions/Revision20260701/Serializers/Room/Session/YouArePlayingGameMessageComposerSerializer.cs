using Vortex.Primitives.Messages.Outgoing.Room.Session;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Session;

internal class YouArePlayingGameMessageComposerSerializer(int header)
    : AbstractSerializer<YouArePlayingGameMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        YouArePlayingGameMessageComposer message
    )
    {
        packet.WriteBoolean(message.IsPlaying);
    }
}
