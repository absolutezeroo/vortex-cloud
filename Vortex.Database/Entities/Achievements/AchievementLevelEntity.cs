using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Achievements;

/// <summary>
/// One level of an achievement. <see cref="ProgressRequirement"/> is the <b>cumulative</b> progress
/// needed to complete this level (the client's AchievementData fields are cumulative across levels,
/// matching the ordered thresholds), <see cref="RewardAmount"/>/<see cref="RewardType"/> is the
/// currency payout granted on completion, and <see cref="ScorePoints"/> is how many points this
/// level contributes to the player's achievement score.
/// </summary>
[Table("achievement_levels")]
[Index(nameof(AchievementEntityId), nameof(Level), IsUnique = true)]
public class AchievementLevelEntity : TurboEntity
{
    [Column("achievement_id")]
    public required int AchievementEntityId { get; set; }

    [Column("level")]
    public required int Level { get; set; }

    [Column("badge_code")]
    public required string BadgeCode { get; set; }

    [Column("progress_requirement")]
    public required int ProgressRequirement { get; set; }

    [Column("reward_amount")]
    public int RewardAmount { get; set; }

    [Column("reward_type")]
    public int RewardType { get; set; }

    [Column("score_points")]
    public required int ScorePoints { get; set; }

    [ForeignKey(nameof(AchievementEntityId))]
    public AchievementEntity? AchievementEntity { get; set; }
}
