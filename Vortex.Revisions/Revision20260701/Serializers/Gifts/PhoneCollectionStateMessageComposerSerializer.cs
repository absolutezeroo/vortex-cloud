using Vortex.Primitives.Messages.Outgoing.Gifts;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Gifts;

internal class PhoneCollectionStateMessageComposerSerializer(int header)
    : AbstractSerializer<PhoneCollectionStateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PhoneCollectionStateMessageComposer message
    )
    {
        //
    }
}
