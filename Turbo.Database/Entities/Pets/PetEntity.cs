using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Players;
using Turbo.Database.Entities.Room;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.Database.Entities.Pets;

[Table("pets")]
[Index(nameof(OwnerPlayerEntityId))]
[Index(nameof(RoomEntityId))]
public class PetEntity : TurboEntity
{
    [Column("player_id")]
    public required int OwnerPlayerEntityId { get; set; }

    [Column("room_id")]
    public int? RoomEntityId { get; set; }

    [Column("name")]
    [MaxLength(40)]
    public required string Name { get; set; }

    [Column("type")]
    public required int Type { get; set; }

    [Column("race")]
    public required int Race { get; set; }

    [Column("color")]
    [MaxLength(12)]
    public required string Color { get; set; }

    [Column("gender")]
    [DefaultValue(AvatarGenderType.Male)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required AvatarGenderType Gender { get; set; }

    [Column("level")]
    [DefaultValue(1)]
    public required int Level { get; set; } = 1;

    [Column("experience")]
    [DefaultValue(0)]
    public required int Experience { get; set; }

    [Column("energy")]
    public required int Energy { get; set; }

    [Column("nutrition")]
    public required int Nutrition { get; set; }

    [Column("respect")]
    [DefaultValue(0)]
    public required int Respect { get; set; }

    [Column("x")]
    public required int X { get; set; }

    [Column("y")]
    public required int Y { get; set; }

    [Column("z", TypeName = "double(10,3)")]
    public required double Z { get; set; }

    [Column("direction")]
    public required int Direction { get; set; }

    [ForeignKey(nameof(OwnerPlayerEntityId))]
    public required PlayerEntity OwnerPlayerEntity { get; set; }

    [ForeignKey(nameof(RoomEntityId))]
    public RoomEntity? RoomEntity { get; set; }
}
