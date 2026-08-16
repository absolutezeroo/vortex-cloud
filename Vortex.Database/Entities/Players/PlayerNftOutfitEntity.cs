using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.Players.Avatar;

namespace Vortex.Database.Entities.Players;

/// <summary>
/// The avatar a player is currently wearing whole, and the look to give them back.
/// </summary>
/// <remarks>
/// <para>
/// Wearing one replaces the player's figure outright, so the look they had is kept here to return to.
/// The client relies on it: opening the avatar editor while wearing an avatar loads this figure
/// rather than what the player currently looks like, which is the only way out of the costume.
/// </para>
/// <para>
/// It points at the <em>copy</em>, not the model, because the client identifies what is worn by the
/// copy's token and matches that against the wardrobe it was sent. Pointing at the model would give
/// two players wearing the same avatar the same token, and the editor would then fail to find which
/// tile to light up.
/// </para>
/// <para>
/// One row per player at most, and no row at all is the ordinary state. That absence is meaningful on
/// the wire: the selection message is only sent when a row exists, because the client treats
/// <em>any</em> answer as "an avatar is worn" — its check is against null, and a string read off a
/// packet is never null.
/// </para>
/// </remarks>
[Table("player_nft_outfit")]
[Index(nameof(PlayerEntityId), IsUnique = true)]
public class PlayerNftOutfitEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("player_nft_avatar_id")]
    public required int PlayerNftAvatarEntityId { get; set; }

    [Column("fallback_figure")]
    [MaxLength(FigureString.MaxLength)]
    public required string FallbackFigure { get; set; }

    [Column("fallback_gender")]
    [MaxLength(1)]
    public required string FallbackGender { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }

    [ForeignKey(nameof(PlayerNftAvatarEntityId))]
    public PlayerNftAvatarEntity? PlayerNftAvatarEntity { get; set; }
}
