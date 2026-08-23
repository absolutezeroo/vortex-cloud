using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Catalog;

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
