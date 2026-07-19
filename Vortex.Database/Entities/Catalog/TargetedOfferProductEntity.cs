using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Furniture;

namespace Vortex.Database.Entities.Catalog;

/// <summary>
/// One product in a targeted offer's bundle. <see cref="ProductCode"/> is the display code sent to
/// the client (as a sub-product); <see cref="FurnitureDefinitionEntityId"/> (when set) is the
/// furniture actually granted on purchase, <see cref="Quantity"/> times.
/// </summary>
[Table("targeted_offer_products")]
[Index(nameof(TargetedOfferEntityId))]
public class TargetedOfferProductEntity : VortexEntity
{
    [Column("targeted_offer_id")]
    public required int TargetedOfferEntityId { get; set; }

    [Column("product_code")]
    public required string ProductCode { get; set; }

    [Column("definition_id")]
    public int? FurnitureDefinitionEntityId { get; set; }

    [Column("quantity")]
    [DefaultValue(1)]
    public int Quantity { get; set; } = 1;

    [ForeignKey(nameof(TargetedOfferEntityId))]
    public TargetedOfferEntity? TargetedOfferEntity { get; set; }

    [ForeignKey(nameof(FurnitureDefinitionEntityId))]
    public FurnitureDefinitionEntity? FurnitureDefinition { get; set; }
}
