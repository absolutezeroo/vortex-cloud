using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Room;

namespace Vortex.Database.Entities.Players;

[Table("player_favorite_rooms")]
[Index(nameof(PlayerEntityId), nameof(RoomEntityId), IsUnique = true)]
public class PlayerFavoriteRoomsEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("room_id")]
    public required int RoomEntityId { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }

    [ForeignKey(nameof(RoomEntityId))]
    public required RoomEntity RoomEntity { get; set; }
}
