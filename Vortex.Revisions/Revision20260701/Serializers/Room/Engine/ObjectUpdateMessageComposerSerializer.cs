using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class ObjectUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<ObjectUpdateMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ObjectUpdateMessageComposer message)
    {
        FloorItemSerializer.Serialize(packet, message.FloorItem);
    }
}
