using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

/// <summary>
/// One copy of an avatar, in one player's hands.
/// </summary>
/// <remarks>
/// <para>
/// Handed out at events rather than bought, so this row is the entire acquisition story — and, with
/// no chain to ask, the entire proof of ownership too. Which is why it records who gave it and what
/// for: that is the provenance a chain would otherwise have kept, and the part of it worth keeping.
/// </para>
/// <para>
/// The row's own id is what the client is told, and what it shows after the "#" under the avatar. So
/// a copy is numbered once, at the moment it is given, and keeps that number for good.
/// </para>
/// <para>
/// Deleting the model leaves these behind rather than cascading: taking an avatar back from everyone
/// who earned it is not what unlisting one should mean, so the wardrobe reads through
/// <c>enabled</c> instead.
/// </para>
/// </remarks>
[Table("player_nft_avatars")]
[Index(nameof(PlayerEntityId), nameof(NftAvatarEntityId), IsUnique = true)]
[Index(nameof(NftAvatarEntityId), nameof(SerialNumber))]
public class PlayerNftAvatarEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("nft_avatar_id")]
    public required int NftAvatarEntityId { get; set; }

    /// <summary>Which of the edition this is: the 3 in "3 of 50". Kept rather than counted, so
    /// revoking an earlier copy does not renumber everyone else's.</summary>
    [Column("serial_number")]
    public int SerialNumber { get; set; }

    /// <summary>The staff member who handed it over, when one did.</summary>
    [Column("granted_by_player_id")]
    public int? GrantedByPlayerEntityId { get; set; }

    /// <summary>What it was given for — the event, the contest, the reason. Free text, read by
    /// people; it is the line of provenance that actually gets read back.</summary>
    [Column("grant_note")]
    [MaxLength(190)]
    public string GrantNote { get; set; } = string.Empty;

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }

    [ForeignKey(nameof(NftAvatarEntityId))]
    public NftAvatarEntity? NftAvatarEntity { get; set; }
}
