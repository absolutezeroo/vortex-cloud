using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Catalog;

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
