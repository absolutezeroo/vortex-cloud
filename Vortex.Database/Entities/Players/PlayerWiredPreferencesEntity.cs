using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

[Table("player_wired_preferences")]
[Index(nameof(PlayerEntityId), IsUnique = true)]
public class PlayerWiredPreferencesEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("wired_menu_button")]
    [DefaultValue(false)]
    public required bool WiredMenuButton { get; set; }

    [Column("wired_inspect_button")]
    [DefaultValue(false)]
    public required bool WiredInspectButton { get; set; }

    [Column("play_test_mode")]
    [DefaultValue(false)]
    public required bool PlayTestMode { get; set; }

    [Column("wired_whisper_disabled")]
    [DefaultValue(false)]
    public required bool WiredWhisperDisabled { get; set; }

    [Column("show_all_notifications")]
    [DefaultValue(false)]
    public required bool ShowAllNotifications { get; set; }

    [Column("ui_style")]
    [MaxLength(64)]
    public string UiStyle { get; set; } = string.Empty;

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
