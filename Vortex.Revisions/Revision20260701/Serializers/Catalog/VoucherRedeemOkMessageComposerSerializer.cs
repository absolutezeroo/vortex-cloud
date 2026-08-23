using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class VoucherRedeemOkMessageComposerSerializer(int header)
    : AbstractSerializer<VoucherRedeemOkMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, VoucherRedeemOkMessageComposer message)
    {
        packet.WriteString(message.ProductDescription).WriteString(message.ProductName);
    }
}
