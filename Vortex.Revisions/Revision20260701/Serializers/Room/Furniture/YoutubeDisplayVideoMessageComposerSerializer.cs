using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Furniture;

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
