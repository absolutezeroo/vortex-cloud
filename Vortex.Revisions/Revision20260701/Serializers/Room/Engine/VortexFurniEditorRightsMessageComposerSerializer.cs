using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;

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
