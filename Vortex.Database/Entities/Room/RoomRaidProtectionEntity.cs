using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Room;

/// <summary>
/// One room's raid-protection settings. A room with no row here has never been configured and runs
/// on the defaults — which is most rooms, and the reason this is not eight more columns on
/// <see cref="RoomEntity" />: the navigator reads that table by the thousand and would carry them
/// all for the handful of rooms that set them.
/// </summary>
/// <remarks>
/// <c>IncidentActive</c> from the wire snapshot is deliberately absent. It says whether a raid is
/// being handled at this instant; persisting it would mean a room that fell over mid-raid comes
/// back up still believing it. <see cref="LastRaidAt" /> is the part worth keeping.
/// </remarks>
[Table("room_raid_protection")]
[Index(nameof(RoomEntityId), IsUnique = true)]
public class RoomRaidProtectionEntity : VortexEntity
{
    [Column("room_id")]
    public required int RoomEntityId { get; set; }

    [Column("enabled")]
    public required bool Enabled { get; set; }

    [Column("detection_sensitivity")]
    public required int DetectionSensitivity { get; set; }

    [Column("action_type")]
    public required int ActionType { get; set; }

    [Column("ban_duration_seconds")]
    public required int BanDurationSeconds { get; set; }

    [Column("guard_enabled")]
    public required bool GuardEnabled { get; set; }

    [Column("guard_duration_seconds")]
    public required int GuardDurationSeconds { get; set; }

    [Column("guard_sensitivity")]
    public required int GuardSensitivity { get; set; }

    /// <summary>When this room was last raided, or null if it never has been.</summary>
    [Column("last_raid_at")]
    public DateTime? LastRaidAt { get; set; }

    [ForeignKey(nameof(RoomEntityId))]
    public RoomEntity? RoomEntity { get; set; }
}
