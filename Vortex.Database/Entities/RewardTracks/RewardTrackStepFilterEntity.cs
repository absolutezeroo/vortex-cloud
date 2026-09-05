using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.RewardTracks;

namespace Vortex.Database.Entities.RewardTracks;

/// <summary>
/// One test a signal must pass to satisfy a step. A step's filters are ANDed.
/// </summary>
/// <remarks>
/// <see cref="Value"/> is either a literal or <c>$N</c>, naming an earlier step: that is what
/// "the same furniture" is made of, and it is the whole reason these hang off a step rather than
/// off the task.
/// </remarks>
[Table("reward_track_step_filters")]
[Index(nameof(RewardTrackTaskStepEntityId))]
public class RewardTrackStepFilterEntity : VortexEntity
{
    [Column("step_id")]
    public required int RewardTrackTaskStepEntityId { get; set; }

    /// <summary>Presentation order. Filters are ANDed, so it can never change the answer.</summary>
    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    /// <summary>One of <see cref="RewardTrackFacts"/>.</summary>
    [Column("fact_key")]
    [MaxLength(ContentIdLength)]
    public required string FactKey { get; set; }

    [Column("operator")]
    [DefaultValue(StepFilterOperator.Equals)]
    public required StepFilterOperator Operator { get; set; }

    [Column("value")]
    [MaxLength(512)]
    [DefaultValue("")]
    public string Value { get; set; } = string.Empty;

    [ForeignKey(nameof(RewardTrackTaskStepEntityId))]
    public RewardTrackTaskStepEntity? Step { get; set; }
}
