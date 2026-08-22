using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading.Data;

/// <summary>
/// One stored item in a furniture chest, in the client's <c>ChestStorage</c> layout.
/// </summary>
/// <remarks>
/// Field order mirrors <c>ChestStorage(IMessageDataWrapper)</c> exactly, including the two traps in
/// it: the transaction id is a <b>long</b>, not an int, and the trailing extra is read only when the
/// item is not a wall item. Getting either wrong shifts every item after it in the page.
///
/// The nested <c>ChestItemType</c> is the same three fields the client writes back when asking to
/// withdraw a kind, in the same order — bool, int, string — so the two directions agree.
/// </remarks>
internal static class ChestStorageSerializer
{
    /// <summary>Per-item lock state. Chest locking is the settings half of this feature and is not
    /// modelled yet, so every item reports unlocked rather than a guessed state.</summary>
    private const int Unlocked = 0;

    /// <summary>The transaction that put the item in the chest. There is no transaction log yet —
    /// it is what the chest's own history screen needs — so zero, which is what the client shows
    /// when it has nothing to link to.</summary>
    private const long NoTransaction = 0L;

    public static void Serialize(IServerPacket packet, FurnitureItemSnapshot item)
    {
        bool isWallItem = item.Definition.ProductType == ProductType.Wall;

        packet.WriteInteger(item.ItemId).WriteInteger(Unlocked).WriteLong(NoTransaction);

        // ChestItemType: what the client needs to draw the row and to name the kind back to us.
        packet
            .WriteBoolean(isWallItem)
            .WriteInteger(item.SpriteId)
            .WriteString(
                item.Definition.FurniCategory == FurnitureCategory.Poster
                    ? item.ExtraData
                    : string.Empty
            );

        packet
            .WriteBoolean(item.Definition.CanGroup)
            .WriteInteger((int)item.Definition.FurniCategory);

        StuffDataSnapshotSerializer.Serialize(packet, item.StuffData);

        if (!isWallItem)
        {
            packet.WriteInteger(item.Extra);
        }
    }
}
