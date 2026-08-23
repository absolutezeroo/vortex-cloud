using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class IsOfferGiftableEventMessageComposerSerializer(int header)
    : AbstractSerializer<IsOfferGiftableEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        IsOfferGiftableEventMessageComposer message
    )
    {
        packet.WriteInteger(message.OfferId).WriteBoolean(message.IsGiftable);
    }
}
