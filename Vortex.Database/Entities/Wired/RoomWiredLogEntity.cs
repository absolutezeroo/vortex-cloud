using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Database.Entities.Wired;

[Table("room_wired_logs")]
[Index(nameof(RoomEntityId), nameof(CreatedAt))]
public class RoomWiredLogEntity : VortexEntity
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

    /// <summary>
    /// The execute-stacks chain step that wrote the line, or 0 for a line written outside one.
    /// Not indexed: the table is already indexed by room and time, and a chain is read by filtering
    /// a room's recent lines rather than by looking one up.
    /// </summary>
    [Column("execution_id")]
    public int ExecutionId { get; set; }

    [Column("parent_execution_id")]
    public int ParentExecutionId { get; set; }

    [ForeignKey(nameof(RoomEntityId))]
    public RoomEntity? RoomEntity { get; set; }
}
