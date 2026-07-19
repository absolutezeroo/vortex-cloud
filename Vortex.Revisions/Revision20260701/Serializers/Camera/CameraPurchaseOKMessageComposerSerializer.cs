using Vortex.Primitives.Messages.Outgoing.Camera;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Camera;

internal class CameraPurchaseOKMessageComposerSerializer(int header)
    : AbstractSerializer<CameraPurchaseOKMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CameraPurchaseOKMessageComposer message)
    {
        //
    }
}
