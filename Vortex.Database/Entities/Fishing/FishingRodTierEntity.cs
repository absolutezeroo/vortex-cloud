using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Fishing;

/// <summary>
/// One rod quality tier.
/// </summary>
/// <remarks>
/// <strong>The rod is not the fishing level.</strong> Origins runs them in parallel: the fishing
/// level unlocks zones and nothing else observed, while the rod's quality raises the multipliers and
/// the chance of triggering Hook Havoc. Fusing the two was the second-biggest error in the first
/// design of this system, and keeping them in separate tables is what stops it recurring.
/// </remarks>
[Table("fishing_rod_tiers")]
[Index(nameof(Quality), IsUnique = true)]
public class FishingRodTierEntity : VortexEntity
{
    /// <summary>Counted from 1. Tiers may skip numbers — the client walks them by threshold.</summary>
    [Column("quality")]
    public required int Quality { get; set; }

    /// <summary>Cumulative <em>rod</em> XP at which this tier begins.</summary>
    [Column("xp_threshold")]
    public int XpThreshold { get; set; }

    [Column("name_key")]
    [MaxLength(128)]
    public required string NameKey { get; set; }

    /// <summary>
    /// The carry object shown in the avatar's hand. At or above 1000: below that the client plays
    /// the drinking animation instead of holding the item.
    /// </summary>
    [Column("hand_item_id")]
    public int HandItemId { get; set; } = 1000;

    /// <summary>Thousandths. 1000 is x1.00.</summary>
    [Column("catch_multiplier")]
    public int CatchMultiplier { get; set; } = 1000;

    [Column("golden_multiplier")]
    public int GoldenMultiplier { get; set; } = 1000;

    /// <summary>Tenths of a percent that a catch triggers Hook Havoc.</summary>
    [Column("hook_havoc_chance")]
    public int HookHavocChance { get; set; }
}
