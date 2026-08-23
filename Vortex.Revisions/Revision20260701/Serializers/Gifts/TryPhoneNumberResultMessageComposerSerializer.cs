using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Gifts;

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
