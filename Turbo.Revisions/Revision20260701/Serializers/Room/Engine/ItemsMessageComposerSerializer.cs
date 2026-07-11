using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Snapshots.Furniture;
using Turbo.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Turbo.Revisions.Revision20260701.Serializers.Room.Engine;

internal class ItemsMessageComposerSerializer(int header)
    : AbstractSerializer<ItemsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ItemsMessageComposer message)
    {
        packet.WriteInteger(message.OwnerNames.Count);

        foreach ((PlayerId ownerId, string ownerName) in message.OwnerNames)
        {
            packet.WriteInteger(ownerId);
            packet.WriteString(ownerName);
        }

        packet.WriteInteger(message.WallItems.Length);

        foreach (RoomWallItemSnapshot item in message.WallItems)
        {
            WallItemSerializer.Serialize(packet, item);
        }
    }
}
