using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Players;

namespace Turbo.Database.Entities.Room;

/// <summary>One vote per player per room -- RateFlatMessage. RoomEntity.Score is the running total;
/// this table exists only to reject a repeat vote from the same player.</summary>
[Table("room_ratings")]
[Index(nameof(RoomEntityId), nameof(PlayerEntityId), IsUnique = true)]
public class RoomRatingEntity : TurboEntity
{
    [Column("room_id")]
    public required int RoomEntityId { get; set; }

    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [ForeignKey(nameof(RoomEntityId))]
    public required RoomEntity RoomEntity { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }
}
