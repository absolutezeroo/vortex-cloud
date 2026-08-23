using System;
using System.Collections.Generic;
using FluentAssertions;
using Vortex.Primitives.Quests.Admin;
using Vortex.Progression.Grains;
using Vortex.Progression.Quests;
using Xunit;

namespace Vortex.Players.Tests.Quests;

/// <summary>
///     What the dashboard may save as quest content. These refuse the shapes that produce a feature
///     which looks configured and cannot work — a ladder that never advances, a task nothing can
///     complete.
/// </summary>
public sealed class QuestContentRulesTests
{
    [Fact]
    public void ValidateGoal_RejectsAGoalWithNoRungs()
    {
        // The widget would sit at 0% forever with no way to progress.
        QuestContentRules.ValidateGoal(Goal()).Should().Be("goal_levels_required");
    }

    [Fact]
    public void ValidateGoal_RejectsTwoRungsAtTheSameTotal()
    {
        // Neither can be "next", so the ladder stalls on whichever the ordering happens to pick.
        QuestContentRules
            .ValidateGoal(Goal(Rung(100), Rung(100)))
            .Should()
            .Be("goal_threshold_duplicate");
    }

    [Fact]
    public void ValidateGoal_RejectsANegativeThreshold()
    {
        QuestContentRules.ValidateGoal(Goal(Rung(-1))).Should().Be("goal_threshold_negative");
    }

    [Fact]
    public void ValidateGoal_RejectsAnEmptyCode()
    {
        QuestContentRules
            .ValidateGoal(Goal(Rung(100)) with { Code = "  " })
            .Should()
            .Be("goal_code_required");
    }

    [Fact]
    public void ValidateGoal_AcceptsALadderInAnyOrder()
    {
        // The admin service renumbers by threshold, so an operator typing them out of order is fine.
        QuestContentRules.ValidateGoal(Goal(Rung(500), Rung(100))).Should().BeNull();
    }

    [Fact]
    public void ValidateDailyTask_RejectsATaskWithNoObjective()
    {
        // Nothing would ever advance it, so it can never be completed or claimed.
        QuestContentRules
            .ValidateDailyTask(Task() with { QuestTypeCode = "" })
            .Should()
            .Be("task_quest_type_required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void ValidateDailyTask_RejectsANonPositiveRepeatCount(int repeats)
    {
        QuestContentRules
            .ValidateDailyTask(Task() with { RequiredRepeats = repeats })
            .Should()
            .Be("task_repeats_invalid");
    }

    [Fact]
    public void ValidateDailyTask_RejectsARewardWithNoType()
    {
        QuestContentRules
            .ValidateDailyTask(Task(new DailyTaskRewardSpec(0, "  ", "", 10)))
            .Should()
            .Be("reward_type_required");
    }

    [Fact]
    public void ValidateDailyTask_RejectsARewardOfNothing()
    {
        QuestContentRules
            .ValidateDailyTask(Task(new DailyTaskRewardSpec(0, "credits", "", 0)))
            .Should()
            .Be("reward_amount_invalid");
    }

    [Fact]
    public void ValidateDailyTask_AcceptsATaskWithNoRewardAtAll()
    {
        // A task can be worth doing for the badge count alone; only a malformed reward is refused.
        QuestContentRules.ValidateDailyTask(Task()).Should().BeNull();
    }

    private static CommunityGoalLevelSpec Rung(int threshold) =>
        new(0, threshold, RewardUserLimit: 5);

    private static CommunityGoalSpec Goal(params CommunityGoalLevelSpec[] levels) =>
        new(
            Code: "summer_build",
            CampaignCode: "summer",
            ScorePerQuest: 1,
            Enabled: true,
            EndsAt: null,
            SortOrder: 0,
            Levels: new List<CommunityGoalLevelSpec>(levels)
        );

    private static DailyTaskSpec Task(params DailyTaskRewardSpec[] rewards) =>
        new(
            TaskCode: "visit_rooms",
            QuestTypeCode: "RoomEntry",
            IsBonus: false,
            ImageVersion: "",
            CatalogName: "",
            RequiredRepeats: 3,
            Enabled: true,
            SortOrder: 0,
            Rewards: new List<DailyTaskRewardSpec>(rewards)
        );
}
