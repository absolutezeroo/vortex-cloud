using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Furni;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Furni;

internal class FurniListInvalidateEventMessageComposerSerializer(int header)
    : AbstractSerializer<FurniListInvalidateEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FurniListInvalidateEventMessageComposer message
    )
    {
        //
    }
}
