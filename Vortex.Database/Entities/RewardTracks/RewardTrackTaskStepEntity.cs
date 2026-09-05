using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.RewardTracks;

/// <summary>
/// One action in a task's sequence. "Place a piece of furniture, then turn it" is two steps; the
/// player has to do them in this order for the task to count once.
/// </summary>
/// <remarks>
/// <para>
/// Every task has at least one step. A plain task — the kind that just counts one action — is a
/// sequence of one, which is why there is no second code path: the engine only ever walks steps.
/// </para>
/// <para>
/// Step 0 is also the task's face. The client draws a task's icon from its <c>action_code</c> and
/// there is exactly one per task on the wire, so the task's own action is kept equal to the first
/// step's — the operator sets the sequence, and the picture follows the thing it starts with.
/// </para>
/// </remarks>
[Table("reward_track_task_steps")]
[Index(nameof(RewardTrackTaskEntityId), nameof(StepIndex), IsUnique = true)]
[Index(nameof(ActionCode))]
public class RewardTrackTaskStepEntity : VortexEntity
{
    [Column("task_id")]
    public required int RewardTrackTaskEntityId { get; set; }

    /// <summary>
    /// Zero-based position in the sequence. Unlike a stage index this carries no player watermark —
    /// a player's place in the sequence is a cursor that resets on every completion — so reordering
    /// steps is safe, and the worst it does is make an in-flight player repeat one.
    /// </summary>
    [Column("step_index")]
    public required int StepIndex { get; set; }

    /// <summary>One of <see cref="Primitives.RewardTracks.RewardTrackActions"/>.</summary>
    [Column("action_code")]
    [MaxLength(ContentIdLength)]
    public required string ActionCode { get; set; }

    /// <summary>
    /// Narrows this step to one target — a furniture definition, a room, a Habbicon. Empty means any
    /// occurrence of the action moves the sequence on.
    /// </summary>
    [Column("parameter")]
    [DefaultValue("")]
    [MaxLength(512)]
    public string Parameter { get; set; } = string.Empty;

    [ForeignKey(nameof(RewardTrackTaskEntityId))]
    public RewardTrackTaskEntity? Task { get; set; }
}
