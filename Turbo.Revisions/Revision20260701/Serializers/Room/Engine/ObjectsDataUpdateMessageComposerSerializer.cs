using Turbo.Primitives.Furniture.Snapshots.StuffData;
using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Rooms.Object;
using Turbo.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Turbo.Revisions.Revision20260701.Serializers.Room.Engine;

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
