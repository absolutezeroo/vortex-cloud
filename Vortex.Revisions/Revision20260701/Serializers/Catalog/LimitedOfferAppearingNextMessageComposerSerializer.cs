using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class LimitedOfferAppearingNextMessageComposerSerializer(int header)
    : AbstractSerializer<LimitedOfferAppearingNextMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        LimitedOfferAppearingNextMessageComposer message
    )
    {
        packet
            .WriteInteger(message.AppearsInSeconds)
            .WriteInteger(message.PageId)
            .WriteInteger(message.OfferId)
            .WriteString(message.ProductType);
    }
}
