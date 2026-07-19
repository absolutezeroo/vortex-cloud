using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Achievements;

/// <summary>
/// A player's progress on a single achievement. <see cref="Progress"/> is the cumulative progress
/// counter and <see cref="Level"/> is the number of levels already completed (0 = none completed
/// yet, working toward level 1). The badge the player currently holds is that of the highest
/// completed level.
/// </summary>
[Table("player_achievements")]
[Index(nameof(PlayerEntityId), nameof(AchievementEntityId), IsUnique = true)]
public class PlayerAchievementEntity : TurboEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("achievement_id")]
    public required int AchievementEntityId { get; set; }

    [Column("progress")]
    public int Progress { get; set; }

    [Column("level")]
    public int Level { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }

    [ForeignKey(nameof(AchievementEntityId))]
    public AchievementEntity? AchievementEntity { get; set; }
}
