using System;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Furni.Data;

internal class FurnitureItemSerializer
{
    public static void Serialize(IServerPacket packet, FurnitureItemSnapshot item)
    {
        ProductType type = item.Definition.ProductType;

        packet
            .WriteInteger(item.ItemId)
            .WriteString(type.ToLegacyString().ToUpper())
            .WriteInteger(type == ProductType.Wall ? Math.Abs(item.ItemId) : -Math.Abs(item.ItemId))
            .WriteInteger(item.SpriteId)
            .WriteInteger((int)item.Definition.FurniCategory);

        StuffDataSnapshotSerializer.Serialize(packet, item.StuffData);

        packet
            .WriteBoolean(item.Definition.CanRecycle)
            .WriteBoolean(item.Definition.CanTrade)
            .WriteBoolean(item.Definition.CanGroup)
            .WriteBoolean(item.Definition.CanSell)
            .WriteInteger(item.SecondsToExpiration)
            .WriteBoolean(item.HasRentPeriodStarted)
            .WriteInteger(item.RoomId);

        if (type == ProductType.Floor)
        {
            packet.WriteString(item.SlotId).WriteInteger(item.Extra);
        }
    }
}
