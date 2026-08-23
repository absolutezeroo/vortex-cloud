using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Gifts;

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
