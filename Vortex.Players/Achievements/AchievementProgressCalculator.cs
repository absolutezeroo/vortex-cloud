using System;
using System.Collections.Immutable;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.Players.Achievements;

/// <summary>
/// Pure mapping from a cached achievement definition + a player's stored progress to the wire-ready
/// <see cref="AchievementProgressSnapshot"/>. Kept free of Orleans/DB so the (subtle) level/progress
/// semantics can be unit-tested in isolation.
///
/// Semantics are derived from the WIN63 20260701 client: the wire <c>level</c> is the level the
/// player is currently working toward (completed levels + 1), except once every level is complete
/// where it equals the level count and <c>finalLevel</c> is set. All figures are cumulative.
/// </summary>
public static class AchievementProgressCalculator
{
    /// <summary>Client achievement state: 1 = enabled (the only state seeded definitions use).</summary>
    private const int StateEnabled = 1;

    /// <summary>
    /// Builds the wire view for one achievement. <paramref name="completedLevels"/> is the number of
    /// levels already completed (0 = none) and is clamped to <c>[0, levelCount]</c>;
    /// <paramref name="progress"/> is the cumulative progress counter.
    /// </summary>
    public static AchievementProgressSnapshot Build(
        AchievementDefinitionSnapshot definition,
        int progress,
        int completedLevels
    )
    {
        int levelCount = definition.Levels.Length;
        if (levelCount == 0)
        {
            throw new ArgumentException(
                $"Achievement '{definition.Name}' has no levels.",
                nameof(definition)
            );
        }

        int completed = Math.Clamp(completedLevels, 0, levelCount);
        bool finalLevel = completed >= levelCount;

        // The level being worked toward (or the last level once finished).
        int currentLevelIndex = Math.Min(completed, levelCount - 1);
        AchievementLevelSnapshot currentLevel = definition.Levels[currentLevelIndex];

        int scoreAtStartOfLevel =
            currentLevelIndex >= 1
                ? definition.Levels[currentLevelIndex - 1].ProgressRequirement
                : 0;
        int levelMaxScore = Math.Max(1, currentLevel.ProgressRequirement);

        // Badge held = highest completed level's badge (empty until the first level is completed).
        string badgeCode =
            completed >= 1 ? definition.Levels[completed - 1].BadgeCode : string.Empty;

        // A finished achievement shows a full bar; otherwise the raw cumulative progress.
        int currentProgress = finalLevel ? levelMaxScore : progress;

        return new AchievementProgressSnapshot
        {
            AchievementId = definition.Id,
            Level = finalLevel ? levelCount : completed + 1,
            BadgeCode = badgeCode,
            ScoreAtStartOfLevel = scoreAtStartOfLevel,
            LevelMaxScore = levelMaxScore,
            LevelRewardAmount = currentLevel.RewardAmount,
            LevelRewardType = currentLevel.RewardType,
            CurrentProgress = currentProgress,
            FinalLevel = finalLevel,
            Category = definition.Category,
            SubCategory = string.Empty,
            LevelCount = levelCount,
            DisplayMethod = definition.DisplayMethod,
            State = StateEnabled,
        };
    }

    /// <summary>
    /// Applies <paramref name="amount"/> progress to an achievement, crossing as many levels as the
    /// new cumulative total warrants. Progress is capped at the final level's requirement, and no
    /// change is reported once the achievement is already maxed or the amount is non-positive.
    /// </summary>
    public static AchievementProgressResult ApplyProgress(
        AchievementDefinitionSnapshot definition,
        int currentProgress,
        int currentCompletedLevels,
        int amount
    )
    {
        int levelCount = definition.Levels.Length;
        if (levelCount == 0)
        {
            throw new ArgumentException(
                $"Achievement '{definition.Name}' has no levels.",
                nameof(definition)
            );
        }

        int completed = Math.Clamp(currentCompletedLevels, 0, levelCount);

        // Already maxed out, or nothing to add: no-op.
        if (amount <= 0 || completed >= levelCount)
        {
            return new AchievementProgressResult
            {
                NewProgress = currentProgress,
                NewCompletedLevels = completed,
                ProgressChanged = false,
                LevelUps = ImmutableArray<AchievementLevelUp>.Empty,
            };
        }

        int maxRequirement = definition.Levels[levelCount - 1].ProgressRequirement;
        int newProgress = Math.Min(currentProgress + amount, maxRequirement);

        ImmutableArray<AchievementLevelUp>.Builder levelUps =
            ImmutableArray.CreateBuilder<AchievementLevelUp>();

        int newCompleted = completed;
        while (
            newCompleted < levelCount
            && newProgress >= definition.Levels[newCompleted].ProgressRequirement
        )
        {
            AchievementLevelSnapshot level = definition.Levels[newCompleted];
            string removedBadge =
                newCompleted >= 1 ? definition.Levels[newCompleted - 1].BadgeCode : string.Empty;

            levelUps.Add(
                new AchievementLevelUp
                {
                    Level = level.Level,
                    BadgeCode = level.BadgeCode,
                    RemovedBadgeCode = removedBadge,
                    RewardAmount = level.RewardAmount,
                    RewardType = level.RewardType,
                    ScorePoints = level.ScorePoints,
                }
            );

            newCompleted++;
        }

        return new AchievementProgressResult
        {
            NewProgress = newProgress,
            NewCompletedLevels = newCompleted,
            ProgressChanged = newProgress != currentProgress || levelUps.Count > 0,
            LevelUps = levelUps.ToImmutable(),
        };
    }

    /// <summary>
    /// Achievement-score contribution of one achievement: the sum of the score points of every
    /// completed level.
    /// </summary>
    public static int ComputeScore(AchievementDefinitionSnapshot definition, int completedLevels)
    {
        int completed = Math.Clamp(completedLevels, 0, definition.Levels.Length);
        int score = 0;
        for (int i = 0; i < completed; i++)
        {
            score += definition.Levels[i].ScorePoints;
        }

        return score;
    }
}
