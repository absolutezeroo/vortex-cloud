using Vortex.Primitives.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class YoutubeDisplayPlaylistsMessageComposerSerializer(int header)
    : AbstractSerializer<YoutubeDisplayPlaylistsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        YoutubeDisplayPlaylistsMessageComposer message
    )
    {
        //
    }
}
