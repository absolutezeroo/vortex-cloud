using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Vortex.Players.Achievements;
using Vortex.Primitives.Players.Snapshots;
using Xunit;

namespace Vortex.Players.Tests.Achievements;

public class AchievementProgressCalculatorTests
{
    // Cumulative thresholds 1/10/25/50/100 across 5 levels, score points 1..5.
    private static AchievementDefinitionSnapshot SampleDefinition()
    {
        int[] thresholds = [1, 10, 25, 50, 100];

        return new AchievementDefinitionSnapshot
        {
            Id = 7,
            Name = "RoomEntry",
            Category = "explore",
            DisplayMethod = 0,
            Levels = Enumerable
                .Range(1, thresholds.Length)
                .Select(level => new AchievementLevelSnapshot
                {
                    Level = level,
                    BadgeCode = $"ACH_RoomEntry{level}",
                    ProgressRequirement = thresholds[level - 1],
                    RewardAmount = level * 10,
                    RewardType = 0,
                    ScorePoints = level,
                })
                .ToImmutableArray(),
        };
    }

    [Fact]
    public void Build_FreshPlayer_WorksTowardFirstLevel()
    {
        AchievementProgressSnapshot result = AchievementProgressCalculator.Build(
            SampleDefinition(),
            progress: 0,
            completedLevels: 0
        );

        result.Level.Should().Be(1);
        result.FinalLevel.Should().BeFalse();
        result.BadgeCode.Should().BeEmpty();
        result.ScoreAtStartOfLevel.Should().Be(0);
        result.LevelMaxScore.Should().Be(1);
        result.CurrentProgress.Should().Be(0);
        result.LevelCount.Should().Be(5);
        result.LevelRewardAmount.Should().Be(10);
        result.State.Should().Be(1);
    }

    [Fact]
    public void Build_PartiallyCompleted_ReportsCurrentLevelBandAndHeldBadge()
    {
        // Two levels done, progress sits inside the third level's band [10, 25).
        AchievementProgressSnapshot result = AchievementProgressCalculator.Build(
            SampleDefinition(),
            progress: 18,
            completedLevels: 2
        );

        result.Level.Should().Be(3); // working toward level 3
        result.FinalLevel.Should().BeFalse();
        result.BadgeCode.Should().Be("ACH_RoomEntry2"); // highest completed level's badge
        result.ScoreAtStartOfLevel.Should().Be(10); // start of level 3 = level 2's requirement
        result.LevelMaxScore.Should().Be(25); // level 3's requirement
        result.CurrentProgress.Should().Be(18);
        result.LevelRewardAmount.Should().Be(30); // level 3's reward
    }

    [Fact]
    public void Build_FullyCompleted_ShowsFinalLevelWithFullBar()
    {
        AchievementProgressSnapshot result = AchievementProgressCalculator.Build(
            SampleDefinition(),
            progress: 120,
            completedLevels: 5
        );

        result.Level.Should().Be(5); // equals level count when finished
        result.FinalLevel.Should().BeTrue();
        result.BadgeCode.Should().Be("ACH_RoomEntry5");
        result.ScoreAtStartOfLevel.Should().Be(50); // start of the last level
        result.LevelMaxScore.Should().Be(100);
        result.CurrentProgress.Should().Be(100); // clamped to a full bar
    }

    [Fact]
    public void Build_ClampsCompletedLevelsAboveLevelCount()
    {
        AchievementProgressSnapshot overflowed = AchievementProgressCalculator.Build(
            SampleDefinition(),
            progress: 999,
            completedLevels: 12
        );

        AchievementProgressSnapshot finished = AchievementProgressCalculator.Build(
            SampleDefinition(),
            progress: 999,
            completedLevels: 5
        );

        overflowed.Should().BeEquivalentTo(finished);
    }

    [Fact]
    public void Build_ThrowsWhenDefinitionHasNoLevels()
    {
        AchievementDefinitionSnapshot empty = SampleDefinition() with
        {
            Levels = ImmutableArray<AchievementLevelSnapshot>.Empty,
        };

        Action act = () => AchievementProgressCalculator.Build(empty, 0, 0);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 3)] // 1 + 2
    [InlineData(5, 15)] // 1 + 2 + 3 + 4 + 5
    [InlineData(9, 15)] // clamped to level count
    public void ComputeScore_SumsCompletedLevelPoints(int completedLevels, int expectedScore)
    {
        int score = AchievementProgressCalculator.ComputeScore(SampleDefinition(), completedLevels);

        score.Should().Be(expectedScore);
    }

    [Fact]
    public void ApplyProgress_CrossingOneThreshold_LevelsUpOnce()
    {
        AchievementProgressResult result = AchievementProgressCalculator.ApplyProgress(
            SampleDefinition(),
            currentProgress: 0,
            currentCompletedLevels: 0,
            amount: 1
        );

        result.NewProgress.Should().Be(1);
        result.NewCompletedLevels.Should().Be(1);
        result.ProgressChanged.Should().BeTrue();
        result.LevelUps.Should().HaveCount(1);
        result.LevelUps[0].Level.Should().Be(1);
        result.LevelUps[0].BadgeCode.Should().Be("ACH_RoomEntry1");
        result.LevelUps[0].RemovedBadgeCode.Should().BeEmpty();
        result.LevelUps[0].ScorePoints.Should().Be(1);
    }

    [Fact]
    public void ApplyProgress_WithinLevelBand_MovesBarWithoutLevelUp()
    {
        // One level done (progress 1); add 3 -> 4, still short of level 2's requirement (10).
        AchievementProgressResult result = AchievementProgressCalculator.ApplyProgress(
            SampleDefinition(),
            currentProgress: 1,
            currentCompletedLevels: 1,
            amount: 3
        );

        result.NewProgress.Should().Be(4);
        result.NewCompletedLevels.Should().Be(1);
        result.ProgressChanged.Should().BeTrue();
        result.LeveledUp.Should().BeFalse();
    }

    [Fact]
    public void ApplyProgress_CrossingSeveralThresholds_LevelsUpForEachWithReplacedBadges()
    {
        // From one level done (holds ACH_RoomEntry1), jump to a cumulative 30 -> completes L2 and L3.
        AchievementProgressResult result = AchievementProgressCalculator.ApplyProgress(
            SampleDefinition(),
            currentProgress: 1,
            currentCompletedLevels: 1,
            amount: 29
        );

        result.NewProgress.Should().Be(30);
        result.NewCompletedLevels.Should().Be(3);
        result.LevelUps.Select(l => l.Level).Should().Equal(2, 3);
        result
            .LevelUps.Select(l => l.RemovedBadgeCode)
            .Should()
            .Equal("ACH_RoomEntry1", "ACH_RoomEntry2");
    }

    [Fact]
    public void ApplyProgress_HugeAmount_CapsAtFinalRequirementAndCompletesAll()
    {
        AchievementProgressResult result = AchievementProgressCalculator.ApplyProgress(
            SampleDefinition(),
            currentProgress: 0,
            currentCompletedLevels: 0,
            amount: 9999
        );

        result.NewProgress.Should().Be(100); // capped at the last level's requirement
        result.NewCompletedLevels.Should().Be(5);
        result.LevelUps.Should().HaveCount(5);
    }

    [Fact]
    public void ApplyProgress_AlreadyMaxed_IsNoOp()
    {
        AchievementProgressResult result = AchievementProgressCalculator.ApplyProgress(
            SampleDefinition(),
            currentProgress: 100,
            currentCompletedLevels: 5,
            amount: 50
        );

        result.NewProgress.Should().Be(100);
        result.NewCompletedLevels.Should().Be(5);
        result.ProgressChanged.Should().BeFalse();
        result.LevelUps.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ApplyProgress_NonPositiveAmount_IsNoOp(int amount)
    {
        AchievementProgressResult result = AchievementProgressCalculator.ApplyProgress(
            SampleDefinition(),
            currentProgress: 5,
            currentCompletedLevels: 1,
            amount: amount
        );

        result.ProgressChanged.Should().BeFalse();
        result.NewProgress.Should().Be(5);
        result.NewCompletedLevels.Should().Be(1);
        result.LevelUps.Should().BeEmpty();
    }
}
