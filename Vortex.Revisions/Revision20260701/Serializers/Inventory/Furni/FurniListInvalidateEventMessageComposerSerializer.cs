using Vortex.Primitives.Messages.Outgoing.Inventory.Furni;
using Vortex.Primitives.Packets;

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
