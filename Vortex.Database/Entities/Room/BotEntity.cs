using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Database.Entities.Room;

/// <summary>
/// A bot a player owns. Modelled on <c>PetEntity</c> rather than on furniture: like a pet, a bot is
/// owned by a player, sits either in their inventory or in exactly one room, and carries its own
/// appearance instead of borrowing a furniture definition's.
/// <para>
/// A null <see cref="RoomEntityId"/> means the bot is in the owner's hand; the position columns are
/// only meaningful alongside a room.
/// </para>
/// </summary>
[Table("bots")]
[Index(nameof(OwnerPlayerEntityId))]
[Index(nameof(RoomEntityId))]
public class BotEntity : VortexEntity
{
    [Column("player_id")]
    public required int OwnerPlayerEntityId { get; set; }

    [Column("room_id")]
    public int? RoomEntityId { get; set; }

    [Column("name")]
    [MaxLength(64)]
    public required string Name { get; set; }

    [Column("motto")]
    [MaxLength(128)]
    public string Motto { get; set; } = string.Empty;

    [Column("figure")]
    [MaxLength(255)]
    public required string Figure { get; set; }

    [Column("gender")]
    public required AvatarGenderType Gender { get; set; }

    [Column("x")]
    public int X { get; set; }

    [Column("y")]
    public int Y { get; set; }

    /// <summary>Altitude in hundredths of a tile, matching how the room stores every other Z.</summary>
    [Column("z")]
    public int Z { get; set; }

    [Column("rotation")]
    public Rotation Rotation { get; set; }

    [ForeignKey(nameof(OwnerPlayerEntityId))]
    public PlayerEntity? OwnerPlayerEntity { get; set; }

    [ForeignKey(nameof(RoomEntityId))]
    public RoomEntity? RoomEntity { get; set; }
}
