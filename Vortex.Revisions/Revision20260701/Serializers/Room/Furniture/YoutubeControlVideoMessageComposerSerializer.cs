using Vortex.Primitives.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class YoutubeControlVideoMessageComposerSerializer(int header)
    : AbstractSerializer<YoutubeControlVideoMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        YoutubeControlVideoMessageComposer message
    )
    {
        //
    }
}
