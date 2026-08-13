using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Collectibles.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftClaimsMessageComposerSerializer(int header)
    : AbstractSerializer<NftClaimsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, NftClaimsMessageComposer message)
    {
        packet.WriteInteger(message.Claims.Length);

        foreach (NftClaimSnapshot claim in message.Claims)
        {
            packet
                .WriteString(claim.ClaimId)
                .WriteInteger(claim.Status)
                .WriteInteger(claim.ClaimedAmount)
                .WriteInteger(claim.ClaimLimit)
                .WriteLong(claim.ValidFrom)
                .WriteLong(claim.ValidTo)
                .WriteLong(claim.CreatedAt)
                .WriteLong(claim.UpdatedAt)
                .WriteString(claim.Collection)
                .WriteString(claim.ProductCode)
                .WriteString(claim.Wallet);

            // The claim item is the product struct plus two strings, because the client's class
            // extends the product one: base fields first, in their own order, extras after.
            CollectibleSerialization.WriteProductItem(packet, claim.ClaimItem.Product);

            packet
                .WriteString(claim.ClaimItem.SetId)
                .WriteString(claim.ClaimItem.DefaultCollectionName);
        }
    }
}
