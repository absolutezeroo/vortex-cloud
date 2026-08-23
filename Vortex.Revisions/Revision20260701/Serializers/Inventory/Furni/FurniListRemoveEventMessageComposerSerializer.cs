using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Furni;

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
