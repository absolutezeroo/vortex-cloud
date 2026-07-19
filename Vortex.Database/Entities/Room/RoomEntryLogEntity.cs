using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Room;

/// <summary>One row per room visit. Entry time is <see cref="TurboEntity.CreatedAt"/>; null
/// <see cref="ExitedAt"/> means the visit is still open (or the session ended without a clean
/// exit, e.g. a crash — an accepted, rare imperfection of this kind of log).</summary>
[Table("room_entry_logs")]
[Index(nameof(PlayerEntityId), nameof(RoomEntityId), nameof(ExitedAt))]
public class RoomEntryLogEntity : TurboEntity
{
    [Column("room_id")]
    public required int RoomEntityId { get; set; }

    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("exited_at")]
    public DateTime? ExitedAt { get; set; }

    [ForeignKey(nameof(RoomEntityId))]
    public RoomEntity? RoomEntity { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
