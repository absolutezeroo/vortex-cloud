using Vortex.Primitives.Messages.Outgoing.Camera;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Camera;

internal class ThumbnailStatusMessageComposerSerializer(int header)
    : AbstractSerializer<ThumbnailStatusMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ThumbnailStatusMessageComposer message)
    {
        //
    }
}
