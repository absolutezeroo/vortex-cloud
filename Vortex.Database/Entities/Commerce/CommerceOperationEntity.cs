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
[Index(nameof(PlayerId))]
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

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
