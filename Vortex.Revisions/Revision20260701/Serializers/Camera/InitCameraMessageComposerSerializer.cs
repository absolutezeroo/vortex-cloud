using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Camera;

namespace Vortex.Revisions.Revision20260701.Serializers.Camera;

internal class InitCameraMessageComposerSerializer(int header)
    : AbstractSerializer<InitCameraMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, InitCameraMessageComposer message)
    {
        //
    }
}
