using System;

namespace Vortex.Progression.Achievements;

/// <summary>
/// The decisions behind a resolution statue, kept away from the grain because every one of them is
/// an off-by-one waiting to happen: levels are counted as "completed", the client's countdown is a
/// duration and not a deadline, and a challenge that has run out of time must stop counting as
/// in progress without anything having run at the moment it expired.
/// </summary>
public static class AchievementResolutionRules
{
    /// <summary>Why an offer cannot be picked, or <see cref="ResolutionState.Selectable"/>.</summary>
    /// <param name="completedLevels">Levels the player has already cleared on this achievement.</param>
    /// <param name="levelCount">How many levels the achievement has in total.</param>
    /// <param name="hasChallengeInProgress">Whether a live challenge already targets it, on this
    /// statue or another.</param>
    public static ResolutionState ResolveState(
        int completedLevels,
        int levelCount,
        bool hasChallengeInProgress
    )
    {
        // Order matters: an achievement that is both finished and challenged reads better as
        // "you finished it" than as "you are still working on it".
        if (levelCount > 0 && completedLevels >= levelCount)
        {
            return ResolutionState.AllLevelsCompleted;
        }

        return hasChallengeInProgress
            ? ResolutionState.AlreadyChallenged
            : ResolutionState.Selectable;
    }

    /// <summary>
    /// The level a challenge asks for: the player's current standing plus the offer's step, never
    /// past the last level the achievement actually has. An offset of zero or less would ask for a
    /// level already cleared, so it counts as one.
    /// </summary>
    public static int ResolveTargetLevel(int completedLevels, int levelCount, int targetLevelOffset)
    {
        int step = targetLevelOffset < 1 ? 1 : targetLevelOffset;
        int target = completedLevels + step;

        return levelCount > 0 && target > levelCount ? levelCount : target;
    }

    /// <summary>
    /// Seconds the client should count down. Never negative: the countdown widget takes this as a
    /// duration, and a negative one would run backwards rather than read as expired.
    /// </summary>
    public static int SecondsLeft(DateTime endsAtUtc, DateTime nowUtc)
    {
        double seconds = (endsAtUtc - nowUtc).TotalSeconds;

        if (seconds <= 0)
        {
            return 0;
        }

        return seconds >= int.MaxValue ? int.MaxValue : (int)seconds;
    }

    /// <summary>Whether a challenge is still running — not finished, and not out of time.</summary>
    public static bool IsInProgress(
        DateTime? completedAtUtc,
        DateTime endsAtUtc,
        DateTime nowUtc
    ) => completedAtUtc is null && endsAtUtc > nowUtc;

    /// <summary>
    /// Whether the challenge is now won. Deliberately independent of when the level was reached:
    /// progress is checked on every level-up rather than by a timer, so a player who reaches the
    /// target after the clock ran out has not won.
    /// </summary>
    public static bool IsWon(
        int completedLevels,
        int targetLevel,
        DateTime endsAtUtc,
        DateTime nowUtc
    ) => completedLevels >= targetLevel && endsAtUtc > nowUtc;

    /// <summary>Mirrors <see cref="Primitives.Players.Snapshots.AchievementResolutionState"/>; the
    /// rules stay free of the wire type so they can be read without one.</summary>
    public enum ResolutionState
    {
        Selectable = 0,
        AllLevelsCompleted = 1,
        AlreadyChallenged = 2,
    }
}
