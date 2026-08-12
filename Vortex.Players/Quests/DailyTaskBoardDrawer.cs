using System;
using System.Collections.Generic;
using System.Linq;

namespace Vortex.Players.Quests;

/// <summary>
/// Picks which task definitions make up one player's board for one day. Deterministic on
/// (player, day) — the same pure-rotation trick the daily quest uses — so a reconnect redraws the
/// same board instead of rerolling it until the player likes what they got.
/// </summary>
public static class DailyTaskBoardDrawer
{
    /// <summary>
    /// Draws <paramref name="count"/> ids out of <paramref name="candidateIds"/>. The offset is a
    /// stable hash of player and date, so two players on the same day get different boards and the
    /// same player gets the same one all day.
    /// </summary>
    /// <remarks>
    /// Returns everything when the pool is smaller than the requested count: a hotel with three
    /// tasks configured should hand out three, not fail to draw.
    /// </remarks>
    public static IReadOnlyList<int> Draw(
        IReadOnlyList<int> candidateIds,
        int playerId,
        DateOnly day,
        int count
    )
    {
        if (candidateIds.Count == 0 || count <= 0)
        {
            return [];
        }

        if (candidateIds.Count <= count)
        {
            return [.. candidateIds];
        }

        int offset = Offset(playerId, day, candidateIds.Count);

        return
        [
            .. Enumerable
                .Range(0, count)
                .Select(i => candidateIds[(offset + i) % candidateIds.Count]),
        ];
    }

    /// <summary>
    /// A stable, non-negative index into a pool of <paramref name="modulus"/> entries. Built from
    /// the day number and the player id rather than a random source so it survives a restart.
    /// </summary>
    private static int Offset(int playerId, DateOnly day, int modulus)
    {
        // DayNumber keeps consecutive days adjacent, and the prime spreads players apart so a whole
        // hotel does not share one board.
        long seed = ((long)day.DayNumber * 31L) + ((long)playerId * 131L);

        return (int)(((seed % modulus) + modulus) % modulus);
    }
}
