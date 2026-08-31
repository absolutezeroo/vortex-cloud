using Orleans;

namespace Vortex.Primitives.Fishing;

/// <summary>
/// One fishing level, and the rod that comes with it.
/// </summary>
/// <remarks>
/// Vortex-specific: no AS3 or Habbo equivalent. See the client's
/// <c>docs/vortex-original/fishing.md</c>.
///
/// <para><strong>The rod is not the fishing level.</strong> Origins runs them in parallel: the
/// fishing level unlocks zones and nothing else observed, while the rod's quality raises the
/// multipliers and the chance of triggering Hook Havoc. Fusing the two was the second-biggest error
/// in the first design of this system.</para>
///
/// <para>Tiers may skip numbers, and the client walks them by threshold rather than keying them by
/// tier, so a gap here is harmless.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record FishingRodLevelSnapshot
{
    /// <summary>Quality tier, counted from 1. Not the fishing level.</summary>
    [Id(0)]
    public required int Quality { get; init; }

    /// <summary>Cumulative <em>rod</em> XP at which this tier begins.</summary>
    [Id(1)]
    public required int XpThreshold { get; init; }

    /// <summary>A localisation key, never a display string.</summary>
    [Id(2)]
    public required string NameKey { get; init; }

    /// <summary>
    /// The carry-object id shown in the avatar's hand. At or above 1000, which is above the client's
    /// <c>CARRY_ITEM_LAST_CONSUMABLE</c> — below it the rod would play the drinking animation.
    /// </summary>
    [Id(3)]
    public required int HandItemId { get; init; }

    /// <summary>Thousandths: 1000 is x1.00, 1450 is x1.45. Integer, so no rounding argument.</summary>
    [Id(4)]
    public required int CatchMultiplier { get; init; }

    [Id(5)]
    public required int GoldenMultiplier { get; init; }

    /// <summary>
    /// Tenths of a percent that a catch triggers Hook Havoc. Origins says a better rod improves the
    /// chance; the real numbers are unknown.
    /// </summary>
    [Id(6)]
    public required int HookHavocChance { get; init; }
}
