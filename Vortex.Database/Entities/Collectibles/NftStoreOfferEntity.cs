using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Collectibles;

/// <summary>
/// One thing for sale on the Collectors Guild's Shop tab, priced in emeralds.
/// </summary>
/// <remarks>
/// Nothing here needs a chain, which is why this tab can work on a hotel that will never mint
/// anything: an offer is a furniture classname, a price, and a couple of flags the client uses to
/// decorate it. The client identifies an offer by its product code when it buys, so that is the key
/// rather than the row id.
/// </remarks>
[Table("nft_store_offers")]
[Index(nameof(ProductCode), IsUnique = true)]
public class NftStoreOfferEntity : VortexEntity
{
    /// <summary>The furniture classname sold, and the id the client sends back on purchase.</summary>
    [Column("product_code")]
    [MaxLength(128)]
    public required string ProductCode { get; set; }

    [Column("emerald_price")]
    public int EmeraldPrice { get; set; }

    /// <summary>Drawn larger, at the top of the shop.</summary>
    [Column("is_featured")]
    public bool IsFeatured { get; set; }

    /// <summary>Shown as a limited edition, which is what makes the client display the counter.</summary>
    [Column("is_limited")]
    public bool IsLimited { get; set; }

    /// <summary>How many may ever be sold. Zero means no limit, and is the only value that makes
    /// sense on an offer that is not limited.</summary>
    [Column("mint_limit")]
    public int MintLimit { get; set; }

    /// <summary>How many have been sold. Maintained by the purchase, not by an admin.</summary>
    [Column("sold_count")]
    public int SoldCount { get; set; }

    /// <summary>
    /// Unused for rendering: the client needs the furniture's sprite id, which is derived from the
    /// definition at send time. See the same field on <c>NftCollectionItemEntity</c>.
    /// </summary>
    [Column("item_type_id")]
    [MaxLength(128)]
    public string ItemTypeId { get; set; } = string.Empty;

    /// <summary>Also unused for rendering — it follows from the definition being wall or floor.</summary>
    [Column("product_type_id")]
    public int ProductTypeId { get; set; }

    /// <summary>Collector XP the item is worth, shown on the offer before buying it.</summary>
    [Column("score")]
    public int Score { get; set; }

    [Column("rarity")]
    [MaxLength(32)]
    public string Rarity { get; set; } = string.Empty;

    /// <summary>Off the shelf without losing the row or its sales count.</summary>
    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("sort_order")]
    public int SortOrder { get; set; }
}
