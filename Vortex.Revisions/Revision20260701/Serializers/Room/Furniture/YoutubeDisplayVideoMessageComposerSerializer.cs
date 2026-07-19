using Vortex.Primitives.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class YoutubeDisplayVideoMessageComposerSerializer(int header)
    : AbstractSerializer<YoutubeDisplayVideoMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        YoutubeDisplayVideoMessageComposer message
    )
    {
        //
    }
}
