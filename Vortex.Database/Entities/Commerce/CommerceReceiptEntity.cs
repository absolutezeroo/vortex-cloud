using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Commerce;

/// <summary>
/// Proof that one step of one operation has been applied. The unique index on (operation, step) is
/// the mechanism, not decoration: a replayed step loses the insert and learns from the loss that it
/// already ran.
/// </summary>
/// <remarks>
/// Where it can, the receipt is written inside the same transaction as the mutation it vouches for —
/// the wallet debit already opens one, so its receipt goes in there and the pair is atomic. Where
/// the mutation is in another grain, the receipt is written first and the step made replayable, and
/// which of the two it is gets decided step by step rather than assumed.
/// </remarks>
[Index(nameof(OperationId), nameof(StepKey), IsUnique = true)]
[Table("commerce_receipts")]
public class CommerceReceiptEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("operation_id")]
    public Guid OperationId { get; set; }

    [Column("step_key")]
    [MaxLength(64)]
    public string StepKey { get; set; } = string.Empty;

    /// <summary>
    /// What the step returned, when a replay has to hand back the earlier answer rather than redo the
    /// work — a debit result, a granted item id. Null when the step has no result worth replaying.
    /// </summary>
    [Column("result")]
    [MaxLength(2048)]
    public string? Result { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
