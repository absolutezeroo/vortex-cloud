using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Turbo.Database.Entities.Catalog;

[Table("catalog_ltd_series")]
public class LtdSeriesEntity : TurboEntity
{
    [Column("catalog_product_id")]
    public required int CatalogProductEntityId { get; set; }

    [Column("total_quantity")]
    public required int TotalQuantity { get; set; }

    [Column("remaining_quantity")]
    public required int RemainingQuantity { get; set; }

    [Column("cost_credits")]
    [DefaultValue(0)]
    public required int CostCredits { get; set; }

    [Column("raffle_window_seconds")]
    [DefaultValue(30)]
    public required int RaffleWindowSeconds { get; set; }

    [Column("is_active")]
    [DefaultValue(true)]
    public required bool IsActive { get; set; }

    [Column("has_raffle_finished")]
    [DefaultValue(false)]
    public required bool HasRaffleFinished { get; set; }

    [Column("starts_at")]
    public DateTime? StartsAt { get; set; }

    [Column("ends_at")]
    public DateTime? EndsAt { get; set; }

    [ForeignKey(nameof(CatalogProductEntityId))]
    public required CatalogProductEntity CatalogProductEntity { get; set; }
}
