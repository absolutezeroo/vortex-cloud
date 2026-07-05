using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Turbo.Database.Entities.Room;

/// <summary>One purchased room-ad slot. One row per purchase (a history log, not upserted) --
/// "is this room currently advertised" is whichever row for a room has the latest ExpiresAt in the
/// future.</summary>
[Table("room_advertisements")]
public class RoomAdvertisementEntity : TurboEntity
{
    [Column("room_id")]
    public required int RoomEntityId { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Column("description")]
    [MaxLength(255)]
    public string? Description { get; set; }

    [Column("category_id")]
    public required int CategoryId { get; set; }

    [Column("extended")]
    public required bool Extended { get; set; }

    [Column("expires_at")]
    public required DateTime ExpiresAt { get; set; }

    [ForeignKey(nameof(RoomEntityId))]
    public RoomEntity? RoomEntity { get; set; }
}
