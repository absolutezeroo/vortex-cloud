using System;
using System.Collections.Generic;
using Turbo.Primitives.Quests.Snapshots;

namespace Turbo.Players.Quests;

/// <summary>
/// Picks a player's daily quest deterministically from the daily pool for a given calendar day — the
/// same player gets the same daily quest all day, and it rotates day to day, with no stored
/// assignment. Pure so the rotation can be unit-tested.
/// </summary>
public static class DailyQuestSelector
{
    public static QuestSnapshot? Pick(
        IReadOnlyList<QuestSnapshot> dailyPool,
        int playerId,
        DateOnly date
    )
    {
        int index = PickIndex(dailyPool.Count, playerId, date);
        return index < 0 ? null : dailyPool[index];
    }

    /// <summary>
    /// Index into a daily pool of <paramref name="count"/> quests for this player and day, or -1 when
    /// the pool is empty. Deterministic, so callers working from either snapshots or definitions
    /// agree on which quest is "today's daily".
    /// </summary>
    public static int PickIndex(int count, int playerId, DateOnly date)
    {
        if (count <= 0)
        {
            return -1;
        }

        int seed = HashCode.Combine(playerId, date.DayNumber);
        return (int)((uint)seed % (uint)count);
    }
}
