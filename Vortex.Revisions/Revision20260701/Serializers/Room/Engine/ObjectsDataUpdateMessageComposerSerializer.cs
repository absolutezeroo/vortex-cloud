using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class ObjectsDataUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<ObjectsDataUpdateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ObjectsDataUpdateMessageComposer message
    )
    {
        packet.WriteInteger(message.StuffDatas.Count);

        foreach ((RoomObjectId itemId, StuffDataSnapshot stuffData) in message.StuffDatas)
        {
            packet.WriteInteger((int)itemId);

            StuffDataSnapshotSerializer.Serialize(packet, stuffData);
        }
    }
}
