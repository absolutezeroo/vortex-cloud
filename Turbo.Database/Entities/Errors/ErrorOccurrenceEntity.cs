using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Turbo.Database.Entities.Errors;

[Table("error_occurrences")]
[Index(nameof(GroupId))]
[Index(nameof(OccurredAt))]
[Index(nameof(Fingerprint))]
[Index(nameof(Source), nameof(OccurredAt))]
public sealed class ErrorOccurrenceEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("fingerprint")]
    [MaxLength(64)]
    public required string Fingerprint { get; set; }

    [Column("group_id")]
    public int GroupId { get; set; }

    [Column("occurred_at")]
    public required DateTime OccurredAt { get; set; }

    [Column("source")]
    [MaxLength(64)]
    public required string Source { get; set; }

    [Column("operation")]
    [MaxLength(128)]
    public required string Operation { get; set; }

    [Column("exception_type")]
    [MaxLength(128)]
    public required string ExceptionType { get; set; }

    [Column("message", TypeName = "text")]
    public string? Message { get; set; }

    [Column("stack_trace", TypeName = "text")]
    public string? StackTrace { get; set; }

    [Column("correlation_id")]
    [MaxLength(32)]
    public string? CorrelationId { get; set; }

    [Column("actor_player_id")]
    public long? ActorPlayerId { get; set; }

    [Column("room_id")]
    public int? RoomId { get; set; }

    [Column("session_key")]
    [MaxLength(128)]
    public string? SessionKey { get; set; }

    [Column("remote_ip")]
    [MaxLength(64)]
    public string? RemoteIp { get; set; }
}
