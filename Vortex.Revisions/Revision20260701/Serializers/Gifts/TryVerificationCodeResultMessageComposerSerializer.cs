using Vortex.Primitives.Messages.Outgoing.Gifts;
using Vortex.Primitives.Packets;

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
