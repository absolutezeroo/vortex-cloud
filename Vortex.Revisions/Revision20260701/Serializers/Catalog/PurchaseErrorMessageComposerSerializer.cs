using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class PurchaseErrorMessageComposerSerializer(int header)
    : AbstractSerializer<PurchaseErrorMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PurchaseErrorMessageComposer message)
    {
        //
    }
}
