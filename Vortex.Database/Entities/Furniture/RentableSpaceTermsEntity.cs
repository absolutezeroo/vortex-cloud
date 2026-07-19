using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Catalog;

namespace Vortex.Database.Entities.Furniture;

/// <summary>
/// Owner-configurable price/duration/currency terms for one placed rentable-space furniture
/// instance. 1:1 with <see cref="FurnitureEntity"/>; one row per placed item.
/// Updated at runtime when the room owner reconfigures the space.
/// </summary>
[Table("room_rentable_space_terms")]
[Index(nameof(FurnitureEntityId), IsUnique = true)]
public class RentableSpaceTermsEntity : TurboEntity
{
    [Column("furniture_id")]
    public required int FurnitureEntityId { get; set; }

    [Column("price")]
    public required int Price { get; set; }

    [Column("currency_type_id")]
    public required int CurrencyTypeEntityId { get; set; }

    [Column("rent_duration_seconds")]
    public required int RentDurationSeconds { get; set; }

    [Column("requires_hc")]
    public required bool RequiresHc { get; set; }

    [ForeignKey(nameof(FurnitureEntityId))]
    public required FurnitureEntity FurnitureEntity { get; set; }

    [ForeignKey(nameof(CurrencyTypeEntityId))]
    public required CurrencyTypeEntity CurrencyTypeEntity { get; set; }
}
