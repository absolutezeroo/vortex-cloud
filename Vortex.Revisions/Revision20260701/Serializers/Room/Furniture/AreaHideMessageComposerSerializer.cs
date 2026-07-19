using Vortex.Primitives.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class AreaHideMessageComposerSerializer(int header)
    : AbstractSerializer<AreaHideMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, AreaHideMessageComposer message)
    {
        AreaHideDataSerializer.Serialize(packet, message.AreaHideData);
    }
}
