using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Turbo.Database.Entities.Catalog;

[Table("catalog_frontpage_items")]
public class CatalogFrontPageItemEntity : TurboEntity
{
    [Column("position")]
    public required int Position { get; set; }

    [Column("item_name")]
    [MaxLength(100)]
    public required string ItemName { get; set; }

    [Column("item_promo_image")]
    [MaxLength(200)]
    public required string ItemPromoImage { get; set; }

    [Column("type")]
    [DefaultValue(0)]
    public required int Type { get; set; }

    [Column("catalog_page_location")]
    [MaxLength(200)]
    public string? CatalogPageLocation { get; set; }

    [Column("product_offer_id")]
    public int? ProductOfferId { get; set; }

    [Column("product_code")]
    [MaxLength(100)]
    public string? ProductCode { get; set; }

    [Column("expires_in_seconds")]
    [DefaultValue(0)]
    public required int ExpiresInSeconds { get; set; }
}
