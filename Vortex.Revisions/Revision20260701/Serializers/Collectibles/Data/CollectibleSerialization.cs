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
    /// One collectible as the amount-carrying subclass reads it. The amount lands <em>between</em>
    /// the score and the pet figure — the client reads it through a <c>readAdditionalParams</c>
    /// hook partway down the base struct rather than at the end — so writing it in declaration
    /// order shifts every field after it. Only the collections list reads this shape: its items and
    /// its bonus/reward items. Everything else reads the base struct, which has no amount at all —
    /// use <see cref="WriteBaseProductItem"/> there.
    /// </summary>
    public static void WriteProductItem(IServerPacket packet, CollectibleProductItemSnapshot item)
    {
        packet
            .WriteShort((short)item.ProductTypeId)
            .WriteString(item.ItemTypeId)
            .WriteInteger(item.Score)
            .WriteInteger(item.Amount);

        WriteProductItemTail(packet, item);
    }

    /// <summary>
    /// The same collectible as the client's <em>base</em> class reads it — no amount anywhere,
    /// because the base <c>readAdditionalParams</c> hook is a no-op. The store offers, the loot-box
    /// reward and the claims list's claim item all read this shape; writing the amount here shifts
    /// every field after the score by four bytes.
    /// </summary>
    public static void WriteBaseProductItem(
        IServerPacket packet,
        CollectibleProductItemSnapshot item
    )
    {
        packet
            .WriteShort((short)item.ProductTypeId)
            .WriteString(item.ItemTypeId)
            .WriteInteger(item.Score);

        WriteProductItemTail(packet, item);
    }

    private static void WriteProductItemTail(
        IServerPacket packet,
        CollectibleProductItemSnapshot item
    )
    {
        packet.WriteString(item.PetFigureString).WriteInteger(item.FigureSetIds.Length);

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
