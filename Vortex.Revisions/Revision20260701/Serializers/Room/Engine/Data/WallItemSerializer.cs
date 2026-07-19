using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

internal class WallItemSerializer
{
    public static void Serialize(IServerPacket packet, RoomWallItemSnapshot item)
    {
        packet
            .WriteString(item.ObjectId.ToString())
            .WriteInteger(item.SpriteId)
            .WriteString(item.WallPosition);

        if (item.StuffData is LegacyStuffSnapshot legacy)
        {
            packet.WriteString(legacy.Data);
        }

        packet
            .WriteInteger(-1) // expiration
            .WriteInteger((int)item.UsagePolicy)
            .WriteInteger(item.OwnerId);
    }
}
