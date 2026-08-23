using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

internal class WiredChestItemsMessageComposerSerializer(int header)
    : AbstractSerializer<WiredChestItemsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, WiredChestItemsMessageComposer message)
    {
        packet
            .WriteInteger(message.ChestId)
            .WriteInteger(message.TotalFragments)
            .WriteInteger(message.FragmentNo)
            .WriteInteger(message.Items.Length);

        foreach (FurnitureItemSnapshot item in message.Items)
        {
            ChestStorageSerializer.Serialize(packet, item);
        }
    }
}
