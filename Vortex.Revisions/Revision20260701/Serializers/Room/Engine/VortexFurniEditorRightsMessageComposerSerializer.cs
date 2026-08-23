using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class VortexFurniEditorRightsMessageComposerSerializer(int header)
    : AbstractSerializer<VortexFurniEditorRightsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VortexFurniEditorRightsMessageComposer message
    )
    {
        packet.WriteBoolean(message.CanEdit);
    }
}
