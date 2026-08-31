using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Fishing;

/// <summary>
/// One fish species an operator has defined.
/// </summary>
/// <remarks>
/// <para>
/// Reconstructed from Habbo Origins, which has no client dump — see the client's
/// <c>docs/vortex-original/fishing.md</c> for how well any of this is known. Every number here is a
/// guess until an Origins capture says otherwise, which is exactly why they live in a table an
/// operator can edit rather than in code.
/// </para>
/// <para>
/// <see cref="CatchRate"/> is the whole difficulty model: there is no minigame on an ordinary catch,
/// so nothing a player does changes their odds. A rare fish is rare because it seldom appears and
/// often escapes.
/// </para>
/// </remarks>
[Table("fishing_species")]
[Index(nameof(ZoneId))]
public class FishingSpeciesEntity : VortexEntity
{
    [Column("zone_id")]
    public required int ZoneId { get; set; }

    /// <summary>A localisation key, never a display string.</summary>
    [Column("name_key")]
    [MaxLength(128)]
    public required string NameKey { get; set; }

    /// <summary>Below this fishing level the species is not in the zone's table at all.</summary>
    [Column("required_level")]
    public int RequiredLevel { get; set; }

    /// <summary>1-5, for display only.</summary>
    [Column("rarity_stars")]
    public int RarityStars { get; set; } = 1;

    /// <summary>Tenths of a percent that a bite lands. 850 is 85%.</summary>
    [Column("catch_rate")]
    public int CatchRate { get; set; } = 500;

    /// <summary>Relative weight when the server picks which species appears.</summary>
    [Column("rarity_weight")]
    public int RarityWeight { get; set; } = 100;

    [Column("min_weight")]
    public int MinWeight { get; set; }

    [Column("max_weight")]
    public int MaxWeight { get; set; }

    [Column("xp_reward")]
    public int XpReward { get; set; }

    [Column("golden_xp_bonus")]
    public int GoldenXpBonus { get; set; }

    [Column("currency_reward")]
    public int CurrencyReward { get; set; }

    /// <summary>24-bit mask, bit h set means available during hour h UTC. Default: every hour.</summary>
    [Column("active_hours")]
    public int ActiveHours { get; set; } = 0xFFFFFF;

    /// <summary>7-bit mask, bit 0 is Sunday. Default: every day.</summary>
    [Column("active_weekdays")]
    public int ActiveWeekdays { get; set; } = 0b1111111;

    /// <summary>
    /// Season mask. How Origins encodes a season is unknown; the four-bit reading is a guess, and
    /// the default is "all year" so a hotel that never sets one is not left with an empty table.
    /// </summary>
    [Column("active_seasons")]
    public int ActiveSeasons { get; set; } = 0b1111;
}
