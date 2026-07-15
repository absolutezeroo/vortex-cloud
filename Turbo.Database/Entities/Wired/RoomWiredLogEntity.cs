using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Room;
using Turbo.Primitives.Rooms.Enums.Wired;

namespace Turbo.Database.Entities.Wired;

[Table("room_wired_logs")]
[Index(nameof(RoomEntityId), nameof(CreatedAt))]
public class RoomWiredLogEntity : TurboEntity
{
    [Column("room_id")]
    public required int RoomEntityId { get; set; }

    [Column("log_level")]
    public required WiredLogLevel LogLevel { get; set; }

    [Column("log_source")]
    public required WiredLogSource LogSource { get; set; }

    [Column("message")]
    [MaxLength(500)]
    public required string Message { get; set; }

    [ForeignKey(nameof(RoomEntityId))]
    public RoomEntity? RoomEntity { get; set; }
}
