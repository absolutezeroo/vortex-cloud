using Vortex.Primitives.Messages.Outgoing.Sound;
using Vortex.Primitives.Packets;

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
