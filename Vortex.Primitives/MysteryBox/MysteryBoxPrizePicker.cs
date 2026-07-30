using System;
using System.Collections.Generic;
using Vortex.Primitives.MysteryBox.Snapshots;

namespace Vortex.Primitives.MysteryBox;

/// <summary>
/// Weighted draw over a prize pool. Kept as a pure function (the roll is injected) so the odds are
/// testable and so the outcome is never influenced by anything the client sends.
/// </summary>
public static class MysteryBoxPrizePicker
{
    /// <summary>
    /// Picks a prize from <paramref name="prizes"/> restricted to <paramref name="pool"/> and to
    /// entries whose colour is empty (any) or equal to <paramref name="color"/>. Returns null when
    /// nothing is eligible. <paramref name="roll"/> receives the total weight and must return a
    /// value in <c>[0, total)</c>.
    /// </summary>
    public static MysteryBoxPrizeSnapshot? Pick(
        IReadOnlyList<MysteryBoxPrizeSnapshot> prizes,
        MysteryBoxPrizePool pool,
        string color,
        Func<int, int> roll
    )
    {
        ArgumentNullException.ThrowIfNull(prizes);
        ArgumentNullException.ThrowIfNull(roll);

        string normalizedColor = MysteryBoxColors.Normalize(color);

        List<MysteryBoxPrizeSnapshot> eligible = [];
        int total = 0;

        foreach (MysteryBoxPrizeSnapshot prize in prizes)
        {
            if (prize.Pool != pool || prize.Weight <= 0)
            {
                continue;
            }

            if (
                prize.Color.Length > 0
                && !string.Equals(prize.Color, normalizedColor, StringComparison.Ordinal)
            )
            {
                continue;
            }

            eligible.Add(prize);
            total += prize.Weight;
        }

        if (total <= 0)
        {
            return null;
        }

        int target = roll(total);

        // A caller handing back an out-of-range roll must not silently fall through the loop and
        // return null — that would read as "empty pool" to the caller and swallow a real bug.
        if (target < 0 || target >= total)
        {
            throw new ArgumentOutOfRangeException(
                nameof(roll),
                target,
                $"Roll must fall in [0, {total})."
            );
        }

        foreach (MysteryBoxPrizeSnapshot prize in eligible)
        {
            target -= prize.Weight;

            if (target < 0)
            {
                return prize;
            }
        }

        return eligible[^1];
    }

    /// <summary>Production overload: draws with <see cref="Random.Shared"/>.</summary>
    public static MysteryBoxPrizeSnapshot? Pick(
        IReadOnlyList<MysteryBoxPrizeSnapshot> prizes,
        MysteryBoxPrizePool pool,
        string color
    ) => Pick(prizes, pool, color, Random.Shared.Next);
}
