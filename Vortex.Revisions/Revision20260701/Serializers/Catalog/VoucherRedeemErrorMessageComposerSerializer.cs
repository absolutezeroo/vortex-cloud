using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class VoucherRedeemErrorMessageComposerSerializer(int header)
    : AbstractSerializer<VoucherRedeemErrorMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VoucherRedeemErrorMessageComposer message
    )
    {
        packet.WriteString(message.ErrorCode);
    }
}
