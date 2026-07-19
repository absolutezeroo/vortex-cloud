using Vortex.Primitives.Messages.Outgoing.Camera;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Camera;

internal class CameraStorageUrlMessageComposerSerializer(int header)
    : AbstractSerializer<CameraStorageUrlMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CameraStorageUrlMessageComposer message)
    {
        //
    }
}
