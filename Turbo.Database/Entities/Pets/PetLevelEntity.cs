using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Turbo.Database.Entities.Pets;

[Table("pet_levels")]
[Index(nameof(PetType), nameof(Level), IsUnique = true)]
public class PetLevelEntity : TurboEntity
{
    [Column("pet_type")]
    public required int PetType { get; set; }

    [Column("level")]
    public required int Level { get; set; }

    [Column("experience_required")]
    public required int ExperienceRequired { get; set; }

    [Column("energy_cap")]
    public required int EnergyCap { get; set; }

    [Column("nutrition_cap")]
    public required int NutritionCap { get; set; }
}
