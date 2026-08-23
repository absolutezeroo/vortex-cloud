using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class PurchaseErrorMessageComposerSerializer(int header)
    : AbstractSerializer<PurchaseErrorMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PurchaseErrorMessageComposer message)
    {
        packet.WriteInteger(message.ErrorCode);
    }
}
