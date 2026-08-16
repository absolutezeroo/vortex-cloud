using System;
using System.Collections.Generic;
using System.Linq;

namespace Vortex.Rooms.Wired;

/// <summary>
/// Draws the effects a pile runs when its random add-on is in play: how many, and how hard it tries
/// not to repeat the last firings.
/// </summary>
/// <remarks>
/// The add-on's two sliders are "Pick N effects" and "Avoid effects from last M executions". The
/// second is a preference, not a rule: a pile whose every effect was used recently must still fire,
/// so the avoid-list is dropped rather than the firing.
/// </remarks>
public static class WiredRandomEffectPicker
{
    /// <summary>
    /// <paramref name="count"/> indices drawn from <paramref name="candidateIds"/>, preferring ids
    /// absent from <paramref name="recentlyUsed"/>. Returns the indices rather than the items so the
    /// caller keeps the pile's own order for what it draws.
    /// </summary>
    public static List<int> Pick(
        IReadOnlyList<int> candidateIds,
        int count,
        IReadOnlySet<int> recentlyUsed,
        Random random
    )
    {
        if (candidateIds.Count == 0 || count <= 0)
        {
            return [];
        }

        List<int> preferred =
        [
            .. Enumerable
                .Range(0, candidateIds.Count)
                .Where(index => !recentlyUsed.Contains(candidateIds[index])),
        ];

        // Everything was used recently: the pile still fires, it just cannot honour the preference.
        List<int> pool =
            preferred.Count > 0 ? preferred : [.. Enumerable.Range(0, candidateIds.Count)];

        if (count >= pool.Count)
        {
            pool.Sort();

            return pool;
        }

        List<int> picked = [];

        while (picked.Count < count)
        {
            int index = pool[random.Next(pool.Count)];

            if (picked.Contains(index))
            {
                continue;
            }

            picked.Add(index);
        }

        picked.Sort();

        return picked;
    }
}
