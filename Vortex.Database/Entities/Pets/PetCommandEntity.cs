using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Pets;

[Table("pet_commands")]
[Index(nameof(PetType))]
[Index(nameof(PetType), nameof(CommandId), IsUnique = true)]
public class PetCommandEntity : TurboEntity
{
    [Column("pet_type")]
    public required int PetType { get; set; }

    [Column("command")]
    public required int CommandId { get; set; }

    [Column("level_required")]
    public required int LevelRequired { get; set; }

    [Column("posture")]
    public required string Posture { get; set; }

    [Column("energy_cost")]
    public required int EnergyCost { get; set; }

    [Column("xp_reward")]
    public required int XpReward { get; set; }
}
