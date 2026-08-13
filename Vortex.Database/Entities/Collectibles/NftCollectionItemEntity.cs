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

    /// <summary>
    /// Unused for rendering, and deliberately so: the client reads the item type with
    /// <c>parseInt</c> and needs the furniture's sprite id, which is derived from the definition at
    /// send time rather than stored here. Storing a classname here is what once drew a dragon lamp
    /// as a post-it. Kept only because product kinds that are not furniture — a badge, say — would
    /// need somewhere to put their own identifier.
    /// </summary>
    [Column("item_type_id")]
    [MaxLength(128)]
    public string ItemTypeId { get; set; } = string.Empty;

    /// <summary>Also unused for rendering — see <see cref="ItemTypeId"/>. Which furniture table the
    /// client searches follows from the definition being a wall or a floor item.</summary>
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
