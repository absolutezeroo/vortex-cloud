using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Furniture;

namespace Vortex.Database.Entities.Pets;

[Table("pet_food")]
[Index(nameof(FurnitureDefinitionEntityId), nameof(PetType), IsUnique = true)]
public class PetFoodEntity : VortexEntity
{
    [Column("furniture_definition_id")]
    public required int FurnitureDefinitionEntityId { get; set; }

    [Column("pet_type")]
    public required int PetType { get; set; }

    [Column("nutrition")]
    public required int Nutrition { get; set; }

    [Column("energy")]
    public required int Energy { get; set; }

    /// <summary>Water. Habbo keeps thirst apart from energy, so a drink slakes it and a nap
    /// does not.</summary>
    [Column("thirst")]
    public int Thirst { get; set; }

    [Column("max_uses")]
    public required int MaxUses { get; set; }

    [ForeignKey(nameof(FurnitureDefinitionEntityId))]
    public required FurnitureDefinitionEntity FurnitureDefinitionEntity { get; set; }
}
