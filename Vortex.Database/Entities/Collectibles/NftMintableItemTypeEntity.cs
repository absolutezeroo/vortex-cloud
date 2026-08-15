using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Collectibles;

/// <summary>
/// One kind of furniture a player may convert into a Relic, and what that costs in stamps.
/// </summary>
/// <remarks>
/// <para>
/// Minting needs no chain: the player already owns the piece of furniture, and converting it hands
/// back a collectible asset in its place. What an admin decides here is which furniture is eligible,
/// how many stamps it takes and for how long the offer stands.
/// </para>
/// <para>
/// The window is not decoration. The client disables the collect button whenever
/// <c>endTime</c> has passed, and it compares against a real clock — so a row with no end date is a
/// row nobody can mint. <see cref="EndsAt"/> is therefore required rather than nullable.
/// </para>
/// </remarks>
[Table("nft_mintable_item_types")]
[Index(nameof(ProductCode), IsUnique = true)]
public class NftMintableItemTypeEntity : VortexEntity
{
    /// <summary>The furniture classname that may be converted.</summary>
    [Column("product_code")]
    [MaxLength(128)]
    public required string ProductCode { get; set; }

    /// <summary>What one conversion costs in stamps.</summary>
    [Column("stamp_price")]
    public int StampPrice { get; set; }

    /// <summary>When the offer opens. Shown as the left edge of the tab's countdown bar.</summary>
    [Column("starts_at")]
    public DateTime StartsAt { get; set; }

    /// <summary>When it closes. The client refuses the conversion from this moment on, and so do we
    /// — it is a deadline players can see, not just a filter.</summary>
    [Column("ends_at")]
    public DateTime EndsAt { get; set; }

    /// <summary>Draws the padlock shut and reads "region locked". It is a label only: the real
    /// hotel's regions do not exist here, so nothing is refused because of it.</summary>
    [Column("region_locked")]
    public bool RegionLocked { get; set; }

    /// <summary>Marks the Relic as a limited edition in the tab's item list.</summary>
    [Column("limited_edition")]
    public bool LimitedEdition { get; set; }

    /// <summary>
    /// How many may ever be converted. Zero is no limit, and is the only value that makes sense on
    /// something not marked limited. The cap is counted against the Relics that exist rather than
    /// against a counter column, so deleting the type and recreating it cannot mint the edition
    /// twice.
    /// </summary>
    [Column("edition_size")]
    public int EditionSize { get; set; }

    /// <summary>Off the list without losing the row.</summary>
    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("sort_order")]
    public int SortOrder { get; set; }
}
