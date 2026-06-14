using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Turbo.Database.Entities.Errors;

[Table("error_groups")]
[Index(nameof(Fingerprint), IsUnique = true)]
[Index(nameof(LastSeenAt))]
[Index(nameof(Source), nameof(Operation), nameof(ExceptionType))]
public sealed class ErrorGroupEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("fingerprint")]
    [MaxLength(64)]
    public required string Fingerprint { get; set; }

    [Column("source")]
    [MaxLength(64)]
    public required string Source { get; set; }

    [Column("operation")]
    [MaxLength(128)]
    public required string Operation { get; set; }

    [Column("exception_type")]
    [MaxLength(128)]
    public required string ExceptionType { get; set; }

    [Column("message_signature")]
    [MaxLength(160)]
    public required string MessageSignature { get; set; }

    [Column("sample_message", TypeName = "varchar(255)")]
    [MaxLength(255)]
    public required string SampleMessage { get; set; }

    [Column("first_seen_at")]
    public required DateTime FirstSeenAt { get; set; }

    [Column("last_seen_at")]
    public required DateTime LastSeenAt { get; set; }

    [Column("total_occurrences")]
    public int TotalOccurrences { get; set; }

    [Column("last_actor_player_id")]
    public long? LastActorPlayerId { get; set; }

    [Column("last_room_id")]
    public int? LastRoomId { get; set; }

    [Column("last_correlation_id")]
    [MaxLength(32)]
    public string? LastCorrelationId { get; set; }
}
