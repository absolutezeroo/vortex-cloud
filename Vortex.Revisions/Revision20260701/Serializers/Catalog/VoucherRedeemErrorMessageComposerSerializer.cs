using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class VoucherRedeemErrorMessageComposerSerializer(int header)
    : AbstractSerializer<VoucherRedeemErrorMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VoucherRedeemErrorMessageComposer message
    )
    {
        //
    }
}
