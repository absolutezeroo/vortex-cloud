using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.RewardTracks;

namespace Vortex.Database.Entities.RewardTracks;

/// <summary>
/// One task of one track. There is no table per task type and no class per task: what a task
/// measures is <see cref="ActionCode"/>, how it counts is <see cref="Mode"/>, and how far it goes
/// is its stages.
/// </summary>
[Table("reward_track_tasks")]
[Index(nameof(RewardTrackEntityId), nameof(TaskId), IsUnique = true)]
[Index(nameof(ActionCode))]
public class RewardTrackTaskEntity : VortexEntity
{
    [Column("reward_track_id")]
    public required int RewardTrackEntityId { get; set; }

    /// <summary>Content id, unique within the track. Part of the client's localization stem.</summary>
    [Column("task_id")]
    [MaxLength(ContentIdLength)]
    public required string TaskId { get; set; }

    /// <summary>One of <see cref="RewardTrackActions"/>. Also the client's artwork key.</summary>
    [Column("action_code")]
    [MaxLength(ContentIdLength)]
    public required string ActionCode { get; set; }

    /// <summary>
    /// Narrows the task to one target: a furniture class, a room id, a Habbicon id. Empty means any
    /// occurrence counts.
    /// </summary>
    [Column("parameter")]
    [DefaultValue("")]
    public string Parameter { get; set; } = string.Empty;

    [Column("mode")]
    [DefaultValue(TaskProgressMode.Counter)]
    public TaskProgressMode Mode { get; set; } = TaskProgressMode.Counter;

    [Column("premium")]
    [DefaultValue(false)]
    public bool Premium { get; set; }

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [ForeignKey(nameof(RewardTrackEntityId))]
    public RewardTrackEntity? RewardTrack { get; set; }
}
