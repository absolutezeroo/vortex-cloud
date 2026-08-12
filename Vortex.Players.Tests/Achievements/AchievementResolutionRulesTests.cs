using System;
using FluentAssertions;
using Vortex.Players.Achievements;
using Xunit;
using State = Vortex.Players.Achievements.AchievementResolutionRules.ResolutionState;

namespace Vortex.Players.Tests.Achievements;

/// <summary>
///     The resolution statue. Levels here are counted as "already cleared", which is the convention
///     player_achievements stores and the opposite of the one the achievement wire uses — so every
///     boundary in this file is one the two conventions could quietly disagree on.
/// </summary>
public sealed class AchievementResolutionRulesTests
{
    private static readonly DateTime Now = new(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ResolveState_IsSelectable_WithLevelsLeftAndNothingRunning()
    {
        AchievementResolutionRules
            .ResolveState(completedLevels: 2, levelCount: 5, hasChallengeInProgress: false)
            .Should()
            .Be(State.Selectable);
    }

    [Fact]
    public void ResolveState_IsCompleted_OnTheLastLevel()
    {
        // Exactly on the count, not past it: clearing all five levels means there is no sixth to
        // challenge, and the client would otherwise offer a target the player can never reach.
        AchievementResolutionRules
            .ResolveState(completedLevels: 5, levelCount: 5, hasChallengeInProgress: false)
            .Should()
            .Be(State.AllLevelsCompleted);
    }

    [Fact]
    public void ResolveState_PrefersCompleted_OverAlreadyChallenged()
    {
        // Both are true when a challenge was running as the last level landed. "You finished it"
        // is the more useful of the two reasons, and the only one that stays true.
        AchievementResolutionRules
            .ResolveState(completedLevels: 5, levelCount: 5, hasChallengeInProgress: true)
            .Should()
            .Be(State.AllLevelsCompleted);
    }

    [Fact]
    public void ResolveState_IgnoresTheLevelCount_WhenTheAchievementHasNoLevels()
    {
        // A definition with no levels is broken data, not a finished achievement; calling it
        // completed would grey out a row the operator is still setting up.
        AchievementResolutionRules
            .ResolveState(completedLevels: 0, levelCount: 0, hasChallengeInProgress: false)
            .Should()
            .Be(State.Selectable);
    }

    [Theory]
    [InlineData(2, 5, 1, 3)]
    [InlineData(0, 5, 3, 3)]
    [InlineData(4, 5, 3, 5)] // clamped to the last level rather than asking for level 7
    [InlineData(2, 5, 0, 3)] // an offset of zero would target a level already cleared
    [InlineData(2, 5, -4, 3)]
    public void ResolveTargetLevel(int completed, int levelCount, int offset, int expected)
    {
        AchievementResolutionRules
            .ResolveTargetLevel(completed, levelCount, offset)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void SecondsLeft_IsZero_OnceTheDeadlineHasPassed()
    {
        // Not negative: the client feeds this to a countdown widget as a duration, so a negative
        // would animate backwards instead of reading as over.
        AchievementResolutionRules.SecondsLeft(Now.AddMinutes(-5), Now).Should().Be(0);
    }

    [Fact]
    public void SecondsLeft_CountsDownToTheDeadline()
    {
        AchievementResolutionRules.SecondsLeft(Now.AddHours(2), Now).Should().Be(7200);
    }

    [Fact]
    public void IsInProgress_IsFalse_ForAChallengeThatRanOut()
    {
        AchievementResolutionRules
            .IsInProgress(completedAtUtc: null, endsAtUtc: Now.AddSeconds(-1), nowUtc: Now)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void IsInProgress_IsFalse_OnceCompleted()
    {
        AchievementResolutionRules
            .IsInProgress(completedAtUtc: Now, endsAtUtc: Now.AddDays(3), nowUtc: Now)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void IsWon_NeedsTheTargetAndTheClock()
    {
        AchievementResolutionRules
            .IsWon(completedLevels: 3, targetLevel: 3, endsAtUtc: Now.AddHours(1), nowUtc: Now)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void IsWon_IsFalse_WhenTheTargetIsReachedTooLate()
    {
        // Progress is settled on level-up rather than by a timer, so a challenge whose deadline
        // passed while nobody was looking must not pay out on the next level.
        AchievementResolutionRules
            .IsWon(completedLevels: 9, targetLevel: 3, endsAtUtc: Now.AddSeconds(-1), nowUtc: Now)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void IsWon_IsTrue_WhenTheTargetIsOvershot()
    {
        // Several levels can land in one progress call; the challenge is won, not skipped.
        AchievementResolutionRules
            .IsWon(completedLevels: 5, targetLevel: 3, endsAtUtc: Now.AddHours(1), nowUtc: Now)
            .Should()
            .BeTrue();
    }
}
