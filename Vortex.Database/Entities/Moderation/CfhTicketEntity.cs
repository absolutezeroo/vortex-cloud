using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Moderation;

namespace Vortex.Database.Entities.Moderation;

/// <summary>A single CFH report, scoped to player reports only (no forum/photo/thread ids on this
/// entity — Daybreak's ModToolIssue overloads one table with reported-forum-content columns too,
/// which the user explicitly flagged as a design smell not to replicate).</summary>
[Table("cfh_tickets")]
[Index(nameof(State))]
public class CfhTicketEntity : VortexEntity
{
    [Column("state")]
    public required CfhTicketState State { get; set; }

    [Column("topic_id")]
    public required int CfhTopicEntityId { get; set; }

    [Column("reporter_player_id")]
    public required int ReporterPlayerEntityId { get; set; }

    [Column("reported_player_id")]
    public required int ReportedPlayerEntityId { get; set; }

    [Column("room_id")]
    public int? RoomEntityId { get; set; }

    [Column("message")]
    [MaxLength(500)]
    public required string Message { get; set; }

    /// <summary>JSON array of the (userId, text) evidence lines the reporter selected from their
    /// local chat history — write-once, never queried relationally, so a blob column is simpler
    /// than a child table for this.</summary>
    [Column("evidence_json")]
    [MaxLength(4000)]
    public string? EvidenceJson { get; set; }

    [Column("picker_player_id")]
    public int? PickerPlayerEntityId { get; set; }

    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }

    [Column("close_reason")]
    public CfhTicketCloseReason? CloseReason { get; set; }

    [Column("sanctioned")]
    public bool Sanctioned { get; set; }

    [ForeignKey(nameof(CfhTopicEntityId))]
    public CfhTopicEntity? CfhTopicEntity { get; set; }

    [ForeignKey(nameof(ReporterPlayerEntityId))]
    public PlayerEntity? ReporterPlayerEntity { get; set; }

    [ForeignKey(nameof(ReportedPlayerEntityId))]
    public PlayerEntity? ReportedPlayerEntity { get; set; }

    [ForeignKey(nameof(RoomEntityId))]
    public RoomEntity? RoomEntity { get; set; }

    [ForeignKey(nameof(PickerPlayerEntityId))]
    public PlayerEntity? PickerPlayerEntity { get; set; }
}
