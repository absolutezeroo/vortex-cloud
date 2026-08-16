using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

/// <summary>
/// One figure set a player has unlocked, and the clothing furni that handed it over.
/// </summary>
/// <remarks>
/// <para>
/// Both halves of what the client is told at login come from this one table: the set ids it needs to
/// let the avatar editor offer, and the distinct classnames it needs to recognise a clothing furni
/// the account has already redeemed. Keeping them as one row per unlocked set means the two lists
/// can never disagree with each other.
/// </para>
/// <para>
/// The set id is written at redemption rather than looked up later. The mapping is regenerated from
/// the hotel's assets, and a furni whose <c>customparams</c> change afterwards must not silently
/// take back a set somebody already owns.
/// </para>
/// </remarks>
[Table("player_clothing")]
// Keyed by the furni as well as the set, not by the set alone: two different clothing furni can
// grant the same set, and the client asks "have I bound *this* furniture?" by classname. Collapsing
// them would leave the second furni unbound forever -- the client would wait five seconds for a
// name that never arrives and drop the outfit without a word.
[Index(
    nameof(PlayerEntityId),
    nameof(FigureSetId),
    nameof(ProductCode),
    IsUnique = true,
    Name = "IX_player_clothing_player_set_product"
)]
public class PlayerClothingEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("figure_set_id")]
    public required int FigureSetId { get; set; }

    /// <summary>
    /// The classname of the furni that granted it. This is what the client compares against — it
    /// asks "have I already bound this furniture?" by name — so it is stored rather than derived
    /// through a definition that may since have been deleted.
    /// </summary>
    [Column("product_code")]
    [MaxLength(128)]
    public required string ProductCode { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
