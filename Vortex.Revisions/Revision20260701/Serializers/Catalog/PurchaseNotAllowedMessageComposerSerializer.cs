using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class PurchaseNotAllowedMessageComposerSerializer(int header)
    : AbstractSerializer<PurchaseNotAllowedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PurchaseNotAllowedMessageComposer message
    )
    {
        packet.WriteInteger((int)message.ErrorType);
    }
}
