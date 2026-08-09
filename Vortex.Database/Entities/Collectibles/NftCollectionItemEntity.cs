using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Collectibles;

/// <summary>
/// One piece of a collection. Matched to what a player owns by <see cref="ProductCode"/>, which is
/// the furniture definition's classname — the same string the catalogue and furnidata use.
/// </summary>
[Table("nft_collection_items")]
[Index(nameof(NftCollectionEntityId), nameof(ProductCode), IsUnique = true)]
public class NftCollectionItemEntity : VortexEntity
{
    [Column("collection_id")]
    public required int NftCollectionEntityId { get; set; }

    [Column("product_code")]
    [MaxLength(128)]
    public required string ProductCode { get; set; }

    /// <summary>What the client shows it as; falls back to the product code when left empty.</summary>
    [Column("item_type_id")]
    [MaxLength(128)]
    public string ItemTypeId { get; set; } = string.Empty;

    [Column("product_type_id")]
    public int ProductTypeId { get; set; }

    /// <summary>What owning this one is worth towards the collection.</summary>
    [Column("score")]
    public int Score { get; set; } = 1;

    [Column("rarity")]
    [MaxLength(32)]
    public string Rarity { get; set; } = string.Empty;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [ForeignKey(nameof(NftCollectionEntityId))]
    public NftCollectionEntity? NftCollection { get; set; }
}
