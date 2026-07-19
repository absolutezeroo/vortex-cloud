using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

[Table("player_navigator_preferences")]
[Index(nameof(PlayerEntityId), IsUnique = true)]
public class PlayerNavigatorPreferencesEntity : TurboEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("window_x")]
    [DefaultValue(427)]
    public int WindowX { get; set; } = 427;

    [Column("window_y")]
    [DefaultValue(41)]
    public int WindowY { get; set; } = 41;

    [Column("window_width")]
    [DefaultValue(425)]
    public int WindowWidth { get; set; } = 425;

    [Column("window_height")]
    [DefaultValue(400)]
    public int WindowHeight { get; set; } = 400;

    [Column("left_pane_hidden")]
    [DefaultValue(false)]
    public bool LeftPaneHidden { get; set; } = false;

    [Column("results_mode")]
    [DefaultValue(1)]
    public int ResultsMode { get; set; } = 1;

    [Column("home_room_id")]
    public int? HomeRoomId { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
