using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Attributes;
using Turbo.Primitives.Observability;

namespace Turbo.Database.Entities.Audit;

/// <summary>
/// Durable, append-only ledger of currency movements (one row per delta, with the resulting balance).
/// Enables economic reconciliation and duplication detection. Like the audit spine it carries no
/// foreign key to players so it survives deletion and supports RGPD anonymisation.
/// </summary>
[Table("economy_ledger")]
[Index(nameof(PlayerId), nameof(OccurredAt))]
[Index(nameof(CorrelationId))]
public class EconomyLedgerEntity : TurboEntity
{
    [Column("occurred_at")]
    public required DateTime OccurredAt { get; set; }

    [Column("player_id")]
    public required long PlayerId { get; set; }

    [Column("currency")]
    [MaxLength(32)]
    public required string Currency { get; set; }

    [Column("activity_point_type")]
    public int? ActivityPointType { get; set; }

    [Column("delta")]
    public required long Delta { get; set; }

    [Column("balance_after")]
    public required long BalanceAfter { get; set; }

    [Column("reason")]
    [EnumStorage(typeof(string))]
    [MaxLength(24)]
    public required EconomyReason Reason { get; set; }

    [Column("correlation_id")]
    [MaxLength(32)]
    public string? CorrelationId { get; set; }

    [Column("ref_id")]
    public long? RefId { get; set; }
}
