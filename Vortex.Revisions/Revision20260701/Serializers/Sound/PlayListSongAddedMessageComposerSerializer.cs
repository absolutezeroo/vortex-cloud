using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class PlayListSongAddedMessageComposerSerializer(int header)
    : AbstractSerializer<PlayListSongAddedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PlayListSongAddedMessageComposer message
    )
    {
        //
    }
}
