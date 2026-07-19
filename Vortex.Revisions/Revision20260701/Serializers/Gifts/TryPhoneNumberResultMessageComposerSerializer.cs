using Vortex.Primitives.Messages.Outgoing.Gifts;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Gifts;

internal class TryPhoneNumberResultMessageComposerSerializer(int header)
    : AbstractSerializer<TryPhoneNumberResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        TryPhoneNumberResultMessageComposer message
    )
    {
        //
    }
}
