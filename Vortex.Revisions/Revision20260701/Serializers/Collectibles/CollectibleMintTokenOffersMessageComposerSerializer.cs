using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class CollectibleMintTokenOffersMessageComposerSerializer(int header)
    : AbstractSerializer<CollectibleMintTokenOffersMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CollectibleMintTokenOffersMessageComposer message
    )
    {
        packet.WriteInteger(message.Offers.Length);

        foreach (MintTokenOfferSnapshot offer in message.Offers)
        {
            packet
                .WriteInteger(offer.OfferId)
                .WriteString(offer.ProductCode)
                .WriteInteger(offer.SilverPrice)
                .WriteInteger(offer.AmountTokens);
        }
    }
}
