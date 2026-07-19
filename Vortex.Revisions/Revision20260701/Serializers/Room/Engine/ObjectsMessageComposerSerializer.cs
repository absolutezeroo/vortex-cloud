using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class ObjectsMessageComposerSerializer(int header)
    : AbstractSerializer<ObjectsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ObjectsMessageComposer message)
    {
        packet.WriteInteger(message.OwnerNames.Count);

        foreach ((PlayerId ownerId, string ownerName) in message.OwnerNames)
        {
            packet.WriteInteger(ownerId);
            packet.WriteString(ownerName);
        }

        packet.WriteInteger(message.FloorItems.Length);

        foreach (RoomFloorItemSnapshot item in message.FloorItems)
        {
            FloorItemSerializer.Serialize(packet, item);
        }
    }
}
