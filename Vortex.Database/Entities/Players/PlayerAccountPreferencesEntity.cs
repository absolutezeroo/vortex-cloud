using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

/// <summary>
/// Per-player account preferences echoed back to the client in the account-preferences packet on
/// login (volumes, free-flow chat / room-camera / room-invite toggles, UI flags). The preferred
/// chat-bubble style stays on <see cref="PlayerEntity.RoomChatStyleId"/>; everything else the
/// settings UI persists lives here so it survives a reconnect. Mirrors
/// <see cref="PlayerWiredPreferencesEntity"/>.
/// </summary>
[Table("player_account_preferences")]
[Index(nameof(PlayerEntityId), IsUnique = true)]
public class PlayerAccountPreferencesEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("ui_volume")]
    [DefaultValue(100)]
    public required int UiVolume { get; set; }

    [Column("furni_volume")]
    [DefaultValue(100)]
    public required int FurniVolume { get; set; }

    [Column("trax_volume")]
    [DefaultValue(100)]
    public required int TraxVolume { get; set; }

    [Column("free_flow_chat_disabled")]
    [DefaultValue(false)]
    public required bool FreeFlowChatDisabled { get; set; }

    [Column("room_invites_ignored")]
    [DefaultValue(false)]
    public required bool RoomInvitesIgnored { get; set; }

    [Column("room_camera_follow_disabled")]
    [DefaultValue(false)]
    public required bool RoomCameraFollowDisabled { get; set; }

    // Default 3 = UIFlags.FriendBarExpanded (1) | UIFlags.RoomToolsExpanded (2), the client's default
    // expanded panels.
    [Column("ui_flags")]
    [DefaultValue(3)]
    public required int UiFlags { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
