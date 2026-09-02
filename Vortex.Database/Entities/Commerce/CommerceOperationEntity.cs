using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.Commerce;

namespace Vortex.Database.Entities.Commerce;

/// <summary>
/// One value-moving operation: what it is, who it belongs to, how far it got, and — once it passes
/// its pivot — that it is owed to the player until it completes.
/// </summary>
/// <remarks>
/// Not a <see cref="VortexEntity"/>: the primary key is the operation id the caller mints before
/// anything durable happens, which is the whole point. An auto-increment key would only exist after
/// the first write, and the identity has to exist before it.
/// </remarks>
[Index(nameof(State), nameof(PivotedAt))]
[Index(nameof(RelayedAt))]
[Index(nameof(PlayerId))]
// The retention sweep's predicate: finished, and finished long enough ago. (State, PivotedAt) does
// not serve it -- a completed operation's pivot time says when it became irreversible, not when it
// stopped being interesting, and most rows in the table are Completed so the state alone narrows
// nothing (PERS-RCP-013).
[Index(nameof(State), nameof(UpdatedAt))]
[Table("commerce_operations")]
public class CommerceOperationEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("kind")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public CommerceOperationKind Kind { get; set; }

    [Column("player_id")]
    public int PlayerId { get; set; }

    [Column("state")]
    [DefaultValue(CommerceOperationState.Prepared)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public CommerceOperationState State { get; set; } = CommerceOperationState.Prepared;

    /// <summary>The step being attempted, so an operator reading a stuck row knows where it stopped.</summary>
    [Column("current_step")]
    [MaxLength(64)]
    public string? CurrentStep { get; set; }

    /// <summary>How many times a post-pivot step has been retried. Feeds the escalation to
    /// <see cref="CommerceOperationState.NeedsIntervention"/>.</summary>
    [Column("attempts")]
    [DefaultValue(0)]
    public int Attempts { get; set; }

    [Column("last_error")]
    [MaxLength(512)]
    public string? LastError { get; set; }

    /// <summary>What the operation is about, in a form a human can read: the offer, the item, the
    /// recipient. Enough to reconstruct intent from a stuck row without joining anything.</summary>
    [Column("detail")]
    [MaxLength(1024)]
    public string? Detail { get; set; }

    /// <summary>When the operation became irreversible. Null until it does. The alert reads this.</summary>
    [Column("pivoted_at")]
    public DateTime? PivotedAt { get; set; }

    /// <summary>
    /// The short type name of the critical business event this operation owes, and the event itself
    /// as JSON. Written in the same transaction as the terminal transition.
    /// </summary>
    /// <remarks>
    /// A purchase used to publish its event immediately after succeeding, outside any transaction: a
    /// crash between the two lost it, and with it the quest progress and the daily task it feeds.
    /// Writing the event with the transition and relaying it afterwards is the whole of the outbox
    /// pattern, and it needs no second table because every critical event identified so far belongs
    /// to an operation.
    /// </remarks>
    [Column("relay_type")]
    [MaxLength(128)]
    public string? RelayType { get; set; }

    [Column("relay_payload")]
    [MaxLength(4096)]
    public string? RelayPayload { get; set; }

    /// <summary>When the relay actually published it. Null while it is still owed.</summary>
    [Column("relayed_at")]
    public DateTime? RelayedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
