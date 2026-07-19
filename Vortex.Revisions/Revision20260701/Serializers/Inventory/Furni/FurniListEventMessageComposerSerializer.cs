using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Inventory.Furni;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Inventory.Furni.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Furni;

internal class FurniListEventMessageComposerSerializer(int header)
    : AbstractSerializer<FurniListEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, FurniListEventMessageComposer message)
    {
        packet
            .WriteInteger(message.TotalFragments)
            .WriteInteger(message.CurrentFragment)
            .WriteInteger(message.Items.Length);

        foreach (FurnitureItemSnapshot item in message.Items)
        {
            FurnitureItemSerializer.Serialize(packet, item);
        }
    }
}
