using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

[Table("player_navigator_view_modes")]
[Index(nameof(PlayerEntityId), nameof(SearchCode), IsUnique = true)]
public class PlayerNavigatorViewModeEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("search_code")]
    public required string SearchCode { get; set; }

    [Column("view_mode")]
    public int ViewMode { get; set; } = 0;

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
