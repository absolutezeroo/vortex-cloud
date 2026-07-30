using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.MysteryBox;

/// <summary>
/// One mystery box key held by a player. Keys are not furniture: no key classname exists in
/// furnidata, and the client draws the tracker/dialog key from its own UI assets tinted by the
/// colour the server sends. They are handed out (quest reward, staff grant) and spent when a box is
/// opened, which is recorded by stamping <see cref="ConsumedAt"/> rather than deleting the row, so
/// the grant/spend history survives.
/// </summary>
[Table("player_mystery_box_keys")]
[Index(nameof(PlayerEntityId), nameof(ConsumedAt))]
public class PlayerMysteryBoxKeyEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("color")]
    [MaxLength(32)]
    public required string Color { get; set; }

    /// <summary>Free-form note on where the key came from (e.g. "quest:xmas12", "staff:admin").</summary>
    [Column("source")]
    [MaxLength(64)]
    [DefaultValue("")]
    public string Source { get; set; } = string.Empty;

    /// <summary>Null while the key is held; set the moment it is spent opening a box.</summary>
    [Column("consumed_at")]
    public DateTime? ConsumedAt { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
