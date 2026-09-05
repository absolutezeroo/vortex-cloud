using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.RewardTracks;

/// <summary>One player's progress on one task of one track.</summary>
[Table("player_reward_track_tasks")]
[Index(nameof(PlayerEntityId), nameof(TrackId), nameof(TaskId), IsUnique = true)]
public class PlayerRewardTrackTaskEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("track_id")]
    [MaxLength(ContentIdLength)]
    public required string TrackId { get; set; }

    [Column("task_id")]
    [MaxLength(ContentIdLength)]
    public required string TaskId { get; set; }

    [Column("progress_count")]
    [DefaultValue(0)]
    public int ProgressCount { get; set; }

    /// <summary>
    /// The highest stage already paid for, or -1. A stage pays when a progress update moves this
    /// number up — which is the whole of the "never pay a stage twice" rule, and it holds across a
    /// reconnect, a duplicated event and a retried grain call alike, because none of those can move
    /// a number that is already there.
    /// </summary>
    [Column("highest_paid_level_index")]
    [DefaultValue(-1)]
    public int HighestPaidLevelIndex { get; set; } = -1;

    /// <summary>
    /// Tab-separated keys already counted, for <c>Distinct</c> tasks only; empty for every other
    /// mode. Bounded by the task's own highest requirement: once progress reaches it, nothing more
    /// is recorded, so the column cannot grow with how long a player plays.
    /// </summary>
    /// <summary>
    /// How far into the task's sequence this player has got. Zero for every plain task, because a
    /// sequence of one is finished the moment it is matched. Resets on each completion, so it is a
    /// cursor and never a watermark -- nothing is paid off it.
    /// </summary>
    [Column("current_step")]
    [DefaultValue(0)]
    public int CurrentStep { get; set; }

    /// <summary>
    /// What each satisfied step of the sequence matched, so a later step can point back at it —
    /// the "walk on the furniture you just placed" half. Cleared on every completion, and empty for
    /// every plain task.
    /// </summary>
    [Column("captured_facts")]
    [DefaultValue("")]
    public string CapturedFacts { get; set; } = string.Empty;

    [Column("distinct_keys")]
    [DefaultValue("")]
    public string DistinctKeys { get; set; } = string.Empty;

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
