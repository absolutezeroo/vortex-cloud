using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class ObjectDataUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<ObjectDataUpdateMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ObjectDataUpdateMessageComposer message)
    {
        packet.WriteString(message.ObjectId.ToString());

        StuffDataSnapshotSerializer.Serialize(packet, message.StuffData);
    }
}
