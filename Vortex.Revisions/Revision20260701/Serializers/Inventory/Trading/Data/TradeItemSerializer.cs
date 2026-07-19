using System;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Trading.Data;

/// <summary>Writes a furniture item in the trade window's <c>ItemDataStructure</c> layout, which
/// differs from the inventory furni-list layout: a single <c>isGroupable</c> flag (not the
/// recycle/trade/group/sell quartet), then stuff data, then a creation day/month/year triple, and a
/// trailing "extra" int only for floor ("S") items. Field order mirrors the client's
/// <c>ItemDataStructure(IMessageDataWrapper)</c> constructor exactly.</summary>
internal static class TradeItemSerializer
{
    public static void Serialize(IServerPacket packet, FurnitureItemSnapshot item)
    {
        ProductType type = item.Definition.ProductType;

        packet
            .WriteInteger(item.ItemId)
            .WriteString(type.ToLegacyString().ToUpper())
            .WriteInteger(type == ProductType.Wall ? Math.Abs(item.ItemId) : -Math.Abs(item.ItemId))
            .WriteInteger(item.SpriteId)
            .WriteInteger((int)item.Definition.FurniCategory)
            .WriteBoolean(item.Definition.CanGroup);

        StuffDataSnapshotSerializer.Serialize(packet, item.StuffData);

        // Acquisition date is cosmetic in the trade window and not carried on the item snapshot;
        // emit a zero triple so the wire stays valid without threading a date through the pipeline.
        packet.WriteInteger(0).WriteInteger(0).WriteInteger(0);

        if (type == ProductType.Floor)
        {
            packet.WriteInteger(item.Extra);
        }
    }
}
