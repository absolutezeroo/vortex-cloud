using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Furniture;

namespace Turbo.Database.Entities.Pets;

[Table("pet_food")]
[Index(nameof(FurnitureDefinitionEntityId), IsUnique = true)]
[Index(nameof(PetType))]
public class PetFoodEntity : TurboEntity
{
    [Column("furniture_definition_id")]
    public required int FurnitureDefinitionEntityId { get; set; }

    [Column("pet_type")]
    public required int PetType { get; set; }

    [Column("nutrition")]
    public required int Nutrition { get; set; }

    [ForeignKey(nameof(FurnitureDefinitionEntityId))]
    public required FurnitureDefinitionEntity FurnitureDefinitionEntity { get; set; }
}
