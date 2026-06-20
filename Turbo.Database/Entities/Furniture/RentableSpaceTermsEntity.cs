using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Catalog;

namespace Turbo.Database.Entities.Furniture;

/// <summary>
/// Price/duration/currency terms for a rentable-space furniture type (DATA-MODEL §3.1).
/// 1:1 with <see cref="FurnitureDefinitionEntity"/>; one row per definition that is a
/// rentable space. Cold config — never updated at runtime.
/// </summary>
[Table("rentable_space_terms")]
[Index(nameof(FurnitureDefinitionEntityId), IsUnique = true)]
public class RentableSpaceTermsEntity : TurboEntity
{
    [Column("furniture_definition_id")]
    public required int FurnitureDefinitionEntityId { get; set; }

    [Column("price")]
    public required int Price { get; set; }

    [Column("currency_type_id")]
    public required int CurrencyTypeEntityId { get; set; }

    [Column("rent_duration_seconds")]
    public required int RentDurationSeconds { get; set; }

    [Column("requires_hc")]
    public required bool RequiresHc { get; set; }

    [ForeignKey(nameof(FurnitureDefinitionEntityId))]
    public required FurnitureDefinitionEntity FurnitureDefinitionEntity { get; set; }

    [ForeignKey(nameof(CurrencyTypeEntityId))]
    public required CurrencyTypeEntity CurrencyTypeEntity { get; set; }
}
