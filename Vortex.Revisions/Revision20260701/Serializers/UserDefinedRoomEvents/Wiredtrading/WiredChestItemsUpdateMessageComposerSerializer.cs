using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

internal class WiredChestItemsUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<WiredChestItemsUpdateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredChestItemsUpdateMessageComposer message
    )
    {
        packet.WriteInteger(message.ChestId).WriteInteger(message.RemovedItemIds.Length);

        foreach (int itemId in message.RemovedItemIds)
        {
            packet.WriteInteger(itemId);
        }

        packet.WriteInteger(message.AddedItems.Length);

        foreach (FurnitureItemSnapshot item in message.AddedItems)
        {
            ChestStorageSerializer.Serialize(packet, item);
        }
    }
}
