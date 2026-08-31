using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Fishing;

/// <summary>
/// One player's standing in the fishing skill.
/// </summary>
/// <remarks>
/// <para>
/// Reconstructed from Habbo Origins, which has no client dump — see the client's
/// <c>docs/vortex-original/fishing.md</c>.
/// </para>
/// <para>
/// <strong>Two progressions, not one.</strong> The fishing level unlocks zones; the rod's quality
/// raises the multipliers and the Hook Havoc chance. They advance on separate XP counters and
/// separate curves, which is why there are four columns here and not two. Fusing them was the
/// second-biggest error in the first design of this system.
/// </para>
/// <para>
/// <see cref="CurrencyEarnedOn"/> is what makes the daily cap resettable without a scheduled job:
/// the cap is compared against <see cref="CurrencyEarnedToday"/> only while the stored date is still
/// today, and any later read treats it as zero. A hotel that goes down over midnight therefore comes
/// back with everybody's cap already clear, and nothing had to run while it was off.
/// </para>
/// </remarks>
[Table("fishing_player_state")]
[Index(nameof(PlayerId), IsUnique = true)]
public class FishingPlayerStateEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerId { get; set; }

    /// <summary>Unlocks zones, and nothing else observed in Origins.</summary>
    [Column("fishing_level")]
    public int FishingLevel { get; set; } = 1;

    [Column("fishing_xp")]
    public int FishingXp { get; set; }

    /// <summary>Rod quality tier. Not the fishing level.</summary>
    [Column("rod_quality")]
    public int RodQuality { get; set; } = 1;

    [Column("rod_xp")]
    public int RodXp { get; set; }

    /// <summary>Fish Tokens held. Non-tradeable by design — the firewall to the hotel economy.</summary>
    [Column("currency")]
    public int Currency { get; set; }

    [Column("currency_earned_today")]
    public int CurrencyEarnedToday { get; set; }

    /// <summary>
    /// The UTC date <see cref="CurrencyEarnedToday"/> was accumulated on. A different date means the
    /// counter is stale and reads as zero.
    /// </summary>
    [Column("currency_earned_on")]
    public DateOnly CurrencyEarnedOn { get; set; }

    /// <summary>Lifetime catches, for the records tab's header.</summary>
    [Column("total_catches")]
    public int TotalCatches { get; set; }

    /// <summary>Lifetime Golden Fish, which only Hook Havoc and frenzies produce.</summary>
    [Column("golden_catches")]
    public int GoldenCatches { get; set; }
}
