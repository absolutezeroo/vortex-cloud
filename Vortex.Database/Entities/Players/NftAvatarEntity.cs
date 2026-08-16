using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.Players.Avatar;

namespace Vortex.Database.Entities.Players;

/// <summary>
/// One avatar a player can wear whole, from the editor's own tab.
/// </summary>
/// <remarks>
/// <para>
/// Unlike a clothing furni, which hands over pieces, this is an entire look: put it on and you appear
/// as that character. On the real hotel these are held in a linked wallet and the server only reads
/// what the chain says; here there is no chain, so the row in <c>player_nft_avatars</c> <em>is</em>
/// the ownership — which is why they are given out from the dashboard and not bought.
/// </para>
/// <para>
/// This table is the model. Each copy handed to a player is a row in <c>player_nft_avatars</c>, and
/// that copy is what the client is told about: it carries the copy's own number, not this row's.
/// </para>
/// </remarks>
[Table("nft_avatars")]
[Index(nameof(AvatarCode), IsUnique = true)]
public class NftAvatarEntity : VortexEntity
{
    /// <summary>The staff handle for this avatar, e.g. <c>halloween_2026_vampire</c>. Never leaves
    /// the dashboard — the client is sent the copy's number instead.</summary>
    [Column("avatar_code")]
    [MaxLength(64)]
    public required string AvatarCode { get; set; }

    [Column("name")]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Column("figure")]
    [MaxLength(FigureString.MaxLength)]
    public required string Figure { get; set; }

    /// <summary>The client's own letter, <c>M</c> or <c>F</c>: an avatar is drawn as one or the
    /// other, and wearing it sets the wearer's gender with it.</summary>
    [Column("gender")]
    [MaxLength(1)]
    public required string Gender { get; set; }

    /// <summary>One of <see cref="NftAvatarCollection"/>'s three values. The client switches on it
    /// for the caption and the tile colours, and draws "null" for anything it does not know.</summary>
    [Column("contract_key")]
    [MaxLength(64)]
    public string ContractKey { get; set; } = NftAvatarCollection.Avatar;

    /// <summary>How many copies may exist, or 0 for as many as staff care to give. This is the whole
    /// of the scarcity: no chain enforces it, this number does.</summary>
    [Column("edition_size")]
    public int EditionSize { get; set; }

    /// <summary>Off the list without losing the row, or the copies that point at it.</summary>
    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("sort_order")]
    public int SortOrder { get; set; }
}
