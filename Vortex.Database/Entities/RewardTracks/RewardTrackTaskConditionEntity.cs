using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.RewardTracks;

namespace Vortex.Database.Entities.RewardTracks;

/// <summary>
/// One extra test on a task's incoming signals. A task's conditions are ANDed; a task with none
/// counts every occurrence of its action, which is what almost every task does.
/// </summary>
/// <remarks>
/// A child table rather than a JSON column on the task, for the same reason the stages are one:
/// an operator edits these in a list, the admin service replaces the list wholesale, and a
/// malformed row should fail on the way in rather than on the way out.
/// </remarks>
[Table("reward_track_task_conditions")]
[Index(nameof(RewardTrackTaskEntityId))]
public class RewardTrackTaskConditionEntity : VortexEntity
{
    [Column("task_id")]
    public required int RewardTrackTaskEntityId { get; set; }

    /// <summary>
    /// Presentation order only. Conditions are ANDed, so evaluating them in a different order can
    /// never change the answer — this is so the operator's list stays where they left it.
    /// </summary>
    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [Column("field")]
    [DefaultValue(TaskConditionField.Target)]
    public required TaskConditionField Field { get; set; }

    [Column("operator")]
    [DefaultValue(TaskConditionOperator.Equals)]
    public required TaskConditionOperator Operator { get; set; }

    /// <summary>
    /// The compared value; a comma-separated list for <see cref="TaskConditionOperator.OneOf"/>.
    /// Bounded because it is content an operator types, not a payload.
    /// </summary>
    [Column("value")]
    [MaxLength(512)]
    [DefaultValue("")]
    public string Value { get; set; } = string.Empty;

    [ForeignKey(nameof(RewardTrackTaskEntityId))]
    public RewardTrackTaskEntity? Task { get; set; }
}
