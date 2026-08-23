using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Gifts;

namespace Vortex.Revisions.Revision20260701.Serializers.Gifts;

internal class TryVerificationCodeResultMessageComposerSerializer(int header)
    : AbstractSerializer<TryVerificationCodeResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        TryVerificationCodeResultMessageComposer message
    )
    {
        //
    }
}
