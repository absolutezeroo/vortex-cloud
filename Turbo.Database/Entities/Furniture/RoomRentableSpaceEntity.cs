using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Players;

namespace Turbo.Database.Entities.Furniture;

/// <summary>
/// Runtime state for one placed rentable-space furniture instance (DATA-MODEL §3.2).
/// One row per instance, updated in-place (never replaced). Null renter = available.
/// </summary>
[Table("room_rentable_spaces")]
[Index(nameof(FurnitureEntityId), IsUnique = true)]
[Index(nameof(RenterPlayerEntityId))]
public class RoomRentableSpaceEntity : TurboEntity
{
    [Column("furniture_id")]
    public required int FurnitureEntityId { get; set; }

    [Column("renter_player_id")]
    public int? RenterPlayerEntityId { get; set; }

    [Column("rented_until")]
    public DateTime? RentedUntil { get; set; }

    [ForeignKey(nameof(FurnitureEntityId))]
    public required FurnitureEntity FurnitureEntity { get; set; }

    [ForeignKey(nameof(RenterPlayerEntityId))]
    public PlayerEntity? RenterPlayerEntity { get; set; }
}
