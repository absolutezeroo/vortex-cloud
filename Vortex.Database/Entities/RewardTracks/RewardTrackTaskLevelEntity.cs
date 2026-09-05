using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.RewardTracks;

/// <summary>
/// One stage of one task: reach <see cref="RequiredCount"/> and the track pays
/// <see cref="PointsReward"/>, once.
/// </summary>
[Table("reward_track_task_levels")]
[Index(nameof(RewardTrackTaskEntityId), nameof(LevelIndex), IsUnique = true)]
public class RewardTrackTaskLevelEntity : VortexEntity
{
    [Column("task_id")]
    public required int RewardTrackTaskEntityId { get; set; }

    /// <summary>
    /// Zero-based. The identity a paid stage is recorded against, so it must not be reshuffled while
    /// players hold progress: inserting a stage in the middle re-labels every stage after it, which
    /// is why the admin service bumps the track's content version when it does.
    /// </summary>
    [Column("level_index")]
    public required int LevelIndex { get; set; }

    [Column("required_count")]
    public required int RequiredCount { get; set; }

    [Column("points_reward")]
    [DefaultValue(0)]
    public int PointsReward { get; set; }

    [Column("premium")]
    [DefaultValue(false)]
    public bool Premium { get; set; }

    [ForeignKey(nameof(RewardTrackTaskEntityId))]
    public RewardTrackTaskEntity? Task { get; set; }
}
