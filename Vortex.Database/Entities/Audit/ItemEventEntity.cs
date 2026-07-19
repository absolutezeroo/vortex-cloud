using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Attributes;
using Vortex.Primitives.Observability;

namespace Vortex.Database.Entities.Audit;

/// <summary>
/// Durable, append-only history of an item's lifecycle, indexed by item id so the complete story of a
/// furniture id can be reconstructed for forensics and duplication investigation.
/// </summary>
[Table("item_events")]
[Index(nameof(ItemId), nameof(OccurredAt))]
[Index(nameof(CorrelationId))]
public class ItemEventEntity : VortexEntity
{
    [Column("item_id")]
    public required long ItemId { get; set; }

    [Column("occurred_at")]
    public required DateTime OccurredAt { get; set; }

    [Column("event_type")]
    [EnumStorage(typeof(string))]
    [MaxLength(24)]
    public required ItemEventType EventType { get; set; }

    [Column("actor_player_id")]
    public long? ActorPlayerId { get; set; }

    [Column("from_owner_id")]
    public long? FromOwnerId { get; set; }

    [Column("to_owner_id")]
    public long? ToOwnerId { get; set; }

    [Column("room_id")]
    public int? RoomId { get; set; }

    [Column("correlation_id")]
    [MaxLength(32)]
    public string? CorrelationId { get; set; }

    [Column("data", TypeName = "text")]
    public string? Data { get; set; }
}
