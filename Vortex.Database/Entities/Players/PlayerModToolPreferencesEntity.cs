using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

/// <summary>
/// Where a staff member last left their mod-tool window. One row per player, upserted — the client
/// reports the rectangle on every move and resize, so this is written far more often than read.
/// </summary>
[Table("player_mod_tool_preferences")]
[Index(nameof(PlayerEntityId), IsUnique = true)]
public class PlayerModToolPreferencesEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("window_x")]
    public required int WindowX { get; set; }

    [Column("window_y")]
    public required int WindowY { get; set; }

    [Column("window_width")]
    public required int WindowWidth { get; set; }

    [Column("window_height")]
    public required int WindowHeight { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
