using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Room;

[Table("room_rights")]
[Index(nameof(RoomEntityId), nameof(PlayerEntityId), IsUnique = true)]
public class RoomRightEntity : VortexEntity
{
    [Column("room_id")]
    public required int RoomEntityId { get; set; }

    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    // Optional on the CLR side only -- the relationship stays required because both foreign keys are
    // non-nullable. A grant is written from two ids the grain already holds, so forcing the caller to
    // materialise the full room and player rows (or fake them with `null!`) buys nothing.
    [ForeignKey(nameof(RoomEntityId))]
    public RoomEntity? RoomEntity { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
