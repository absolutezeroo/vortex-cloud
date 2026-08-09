using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles.Data;

/// <summary>
/// The shared collectible structs, in the order the client's own parsers read them. Both are read
/// in more than one place, so they are written in exactly one.
/// </summary>
internal static class CollectibleSerialization
{
    /// <summary>
    /// One collectible. The amount lands <em>between</em> the score and the pet figure — the client
    /// reads it through a <c>readAdditionalParams</c> hook partway down the base struct rather than
    /// at the end — so writing it in declaration order shifts every field after it.
    /// </summary>
    public static void WriteProductItem(IServerPacket packet, CollectibleProductItemSnapshot item)
    {
        packet
            .WriteShort((short)item.ProductTypeId)
            .WriteString(item.ItemTypeId)
            .WriteInteger(item.Score)
            .WriteInteger(item.Amount)
            .WriteString(item.PetFigureString)
            .WriteInteger(item.FigureSetIds.Length);

        foreach (int figureSetId in item.FigureSetIds)
        {
            packet.WriteInteger(figureSetId);
        }

        packet.WriteString(item.ProductCode).WriteString(item.Rarity);
    }

    public static void WriteClaim(IServerPacket packet, CollectibleItemClaimSnapshot claim) =>
        packet
            .WriteString(claim.ClaimId)
            .WriteInteger(claim.ClaimedAmount)
            .WriteInteger(claim.ClaimLimit)
            .WriteShort((short)claim.Status);
}
