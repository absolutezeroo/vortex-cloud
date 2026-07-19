using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class ObjectAddMessageComposerSerializer(int header)
    : AbstractSerializer<ObjectAddMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ObjectAddMessageComposer message)
    {
        FloorItemSerializer.Serialize(packet, message.FloorItem);

        packet.WriteString(message.FloorItem.OwnerName);
    }
}
