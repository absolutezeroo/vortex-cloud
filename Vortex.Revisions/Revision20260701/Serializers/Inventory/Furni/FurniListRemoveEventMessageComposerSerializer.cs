using Vortex.Primitives.Messages.Outgoing.Inventory.Furni;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Furni;

internal class FurniListRemoveEventMessageComposerSerializer(int header)
    : AbstractSerializer<FurniListRemoveEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FurniListRemoveEventMessageComposer message
    )
    {
        packet.WriteInteger(message.ItemId);
    }
}
