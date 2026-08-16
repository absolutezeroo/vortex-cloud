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

        // Always a string, whatever the stuff-data type. A wall item's data is one legacy string on
        // the wire — WIN63's parseItemData (unknowns/_SafePkg_2184/_SafeCls_4408.as) reads
        // string/int/string/string then three ints, unconditionally — unlike a floor item, which
        // carries the full polymorphic blob. Writing nothing for a non-legacy snapshot dropped the
        // field entirely, so the client read the expiration int as a string length and every later
        // field in the packet shifted. A wall photo (MapStuffData) is enough to trigger it.
        //
        // TODO: the empty string is a stand-in. The correct value is the item's legacy projection —
        // `Logic.StuffData.GetLegacyString()`, which is what ItemStateUpdate/ItemDataUpdate already
        // send — but StuffDataSnapshot carries no such field, so it cannot be reached from here
        // without widening the snapshot. Until then a non-legacy wall item renders stateless rather
        // than desyncing the room.
        packet.WriteString(
            item.StuffData is LegacyStuffSnapshot legacy ? legacy.Data : string.Empty
        );

        packet
            .WriteInteger(-1) // expiration
            .WriteInteger((int)item.UsagePolicy)
            .WriteInteger(item.OwnerId);
    }
}
