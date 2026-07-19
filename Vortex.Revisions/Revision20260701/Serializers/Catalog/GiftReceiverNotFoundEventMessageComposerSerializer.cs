using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class GiftReceiverNotFoundEventMessageComposerSerializer(int header)
    : AbstractSerializer<GiftReceiverNotFoundEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GiftReceiverNotFoundEventMessageComposer message
    )
    {
        //
    }
}
