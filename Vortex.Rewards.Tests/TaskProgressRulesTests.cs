using System.Collections.Immutable;
using FluentAssertions;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Snapshots;
using Vortex.RewardTracks.Progression;
using Xunit;

namespace Vortex.Rewards.Tests;

/// <summary>
/// The stage arithmetic. Everything that decides whether a player is paid, and how many times.
/// </summary>
public class TaskProgressRulesTests
{
    private static readonly RewardTrackTaskDefinitionSnapshot ThreeStages = Content.Task(
        levels: [Content.Level(0, 1, 10), Content.Level(1, 5, 20), Content.Level(2, 20, 30)]
    );

    [Fact]
    public void First_progress_advances_and_pays_the_first_stage()
    {
        TaskProgressOutcome outcome = TaskProgressRules.Apply(
            ThreeStages,
            currentProgress: 0,
            highestPaidLevelIndex: -1,
            distinctKeys: "",
            amount: 1,
            target: null,
            premiumUnlocked: false
        );

        outcome.NewProgress.Should().Be(1);
        outcome.StagesPaid.Should().Equal(0);
        outcome.PointsGranted.Should().Be(10);
        outcome.HighestPaidLevelIndex.Should().Be(0);
    }

    [Fact]
    public void Progress_between_stages_pays_nothing()
    {
        TaskProgressOutcome outcome = TaskProgressRules.Apply(
            ThreeStages,
            currentProgress: 1,
            highestPaidLevelIndex: 0,
            distinctKeys: "",
            amount: 1,
            target: null,
            premiumUnlocked: false
        );

        outcome.NewProgress.Should().Be(2);
        outcome.StagesPaid.Should().BeEmpty();
        outcome.PointsGranted.Should().Be(0);
    }

    [Fact]
    public void Landing_exactly_on_a_threshold_pays_it()
    {
        TaskProgressOutcome outcome = TaskProgressRules.Apply(
            ThreeStages,
            currentProgress: 4,
            highestPaidLevelIndex: 0,
            distinctKeys: "",
            amount: 1,
            target: null,
            premiumUnlocked: false
        );

        outcome.NewProgress.Should().Be(5);
        outcome.StagesPaid.Should().Equal(1);
        outcome.PointsGranted.Should().Be(20);
    }

    /// <summary>
    /// A single signal that jumps past two thresholds pays both. Paying only the highest would
    /// silently drop the points of every stage a big grant skipped over.
    /// </summary>
    [Fact]
    public void One_jump_past_several_thresholds_pays_all_of_them()
    {
        TaskProgressOutcome outcome = TaskProgressRules.Apply(
            ThreeStages,
            currentProgress: 0,
            highestPaidLevelIndex: -1,
            distinctKeys: "",
            amount: 50,
            target: null,
            premiumUnlocked: false
        );

        outcome
            .NewProgress.Should()
            .Be(20, "progress is clamped to the task's highest requirement");
        outcome.StagesPaid.Should().Equal(0, 1, 2);
        outcome.PointsGranted.Should().Be(60);
        outcome.HighestPaidLevelIndex.Should().Be(2);
    }

    /// <summary>
    /// The whole idempotency story in one test: the same state applied twice pays once, because the
    /// watermark has already moved past the stage.
    /// </summary>
    [Fact]
    public void A_repeated_signal_never_pays_a_stage_twice()
    {
        TaskProgressOutcome first = TaskProgressRules.Apply(
            ThreeStages,
            currentProgress: 0,
            highestPaidLevelIndex: -1,
            distinctKeys: "",
            amount: 5,
            target: null,
            premiumUnlocked: false
        );

        first.PointsGranted.Should().Be(30);

        // The same event delivered again -- a reconnect, a retried grain call, the commerce relay
        // doing its job.
        TaskProgressOutcome replay = TaskProgressRules.Apply(
            ThreeStages,
            currentProgress: first.NewProgress,
            highestPaidLevelIndex: first.HighestPaidLevelIndex,
            distinctKeys: first.DistinctKeys,
            amount: 5,
            target: null,
            premiumUnlocked: false
        );

        replay.StagesPaid.Should().BeEmpty();
        replay.PointsGranted.Should().Be(0);
    }

    [Fact]
    public void Progress_stops_at_the_last_stage()
    {
        TaskProgressOutcome outcome = TaskProgressRules.Apply(
            ThreeStages,
            currentProgress: 20,
            highestPaidLevelIndex: 2,
            distinctKeys: "",
            amount: 10,
            target: null,
            premiumUnlocked: false
        );

        outcome.NewProgress.Should().Be(20);
        outcome.Changed(20).Should().BeFalse("nothing is written when nothing moved");
    }

    [Fact]
    public void A_distinct_task_counts_each_target_once()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Task(
            mode: TaskProgressMode.Distinct,
            levels: [Content.Level(0, 3, 30)]
        );

        TaskProgressOutcome first = TaskProgressRules.Apply(
            task,
            0,
            -1,
            "",
            1,
            "room-7",
            premiumUnlocked: false
        );

        first.NewProgress.Should().Be(1);

        TaskProgressOutcome again = TaskProgressRules.Apply(
            task,
            first.NewProgress,
            first.HighestPaidLevelIndex,
            first.DistinctKeys,
            1,
            "room-7",
            premiumUnlocked: false
        );

        again.NewProgress.Should().Be(1, "the same room does not count twice");
        again.Changed(first.NewProgress).Should().BeFalse();

        TaskProgressOutcome other = TaskProgressRules.Apply(
            task,
            first.NewProgress,
            first.HighestPaidLevelIndex,
            first.DistinctKeys,
            1,
            "room-8",
            premiumUnlocked: false
        );

        other.NewProgress.Should().Be(2);
    }

    /// <summary>
    /// The key set is bounded by the content: once the task's highest requirement is met there is
    /// nothing left to deduplicate, so nothing more is recorded and the column cannot grow with how
    /// long somebody plays.
    /// </summary>
    [Fact]
    public void A_distinct_task_stops_recording_keys_once_it_is_complete()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Task(
            mode: TaskProgressMode.Distinct,
            levels: [Content.Level(0, 2, 10)]
        );

        TaskProgressOutcome complete = TaskProgressRules.Apply(task, 2, 0, "a\tb", 1, "c", false);

        complete.DistinctKeys.Should().Be("a\tb");
        complete.NewProgress.Should().Be(2);
    }

    /// <summary>
    /// An absolute task reports a total, so it can go down — losing a friend really does take a
    /// "have 5 friends" task back. The stage already paid stays paid: the watermark, not the count,
    /// is what protects it.
    /// </summary>
    [Fact]
    public void An_absolute_task_follows_the_reported_total_both_ways()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Task(
            mode: TaskProgressMode.Absolute,
            levels: [Content.Level(0, 5, 25)]
        );

        TaskProgressOutcome up = TaskProgressRules.Apply(task, 0, -1, "", 5, null, false);

        up.NewProgress.Should().Be(5);
        up.PointsGranted.Should().Be(25);

        TaskProgressOutcome down = TaskProgressRules.Apply(task, 5, 0, "", 3, null, false);

        down.NewProgress.Should().Be(3);
        down.PointsGranted.Should().Be(0);

        TaskProgressOutcome backUp = TaskProgressRules.Apply(task, 3, 0, "", 5, null, false);

        backUp.PointsGranted.Should().Be(0, "the stage was already paid for");
    }

    [Fact]
    public void A_highest_task_never_goes_down()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Task(
            mode: TaskProgressMode.Highest,
            levels: [Content.Level(0, 10, 10)]
        );

        TaskProgressOutcome outcome = TaskProgressRules.Apply(task, 7, -1, "", 3, null, false);

        outcome.NewProgress.Should().Be(7);
    }

    [Fact]
    public void A_task_with_a_parameter_ignores_a_different_target()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Task(parameter: "chair");

        TaskProgressRules.Apply(task, 0, -1, "", 1, "table", false).NewProgress.Should().Be(0);

        TaskProgressRules.Apply(task, 0, -1, "", 1, "chair", false).NewProgress.Should().Be(1);
    }

    [Fact]
    public void A_task_without_a_parameter_takes_any_target()
    {
        TaskProgressRules
            .Apply(Content.Task(), 0, -1, "", 1, "anything", false)
            .NewProgress.Should()
            .Be(1);
    }

    [Fact]
    public void A_premium_task_does_not_advance_for_a_free_player()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Task(premium: true);

        TaskProgressRules
            .Apply(task, 0, -1, "", 1, null, premiumUnlocked: false)
            .NewProgress.Should()
            .Be(0);

        TaskProgressRules
            .Apply(task, 0, -1, "", 1, null, premiumUnlocked: true)
            .NewProgress.Should()
            .Be(1);
    }

    /// <summary>
    /// A free player's progress climbs past a premium stage without being paid for it, and the
    /// watermark stays put — so buying premium later pays the stage they had already earned.
    /// </summary>
    [Fact]
    public void A_premium_stage_is_held_open_until_premium_is_bought()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Task(
            levels: [Content.Level(0, 1, 10), Content.Level(1, 5, 50, premium: true)]
        );

        TaskProgressOutcome free = TaskProgressRules.Apply(task, 0, -1, "", 5, null, false);

        free.NewProgress.Should().Be(5);
        free.StagesPaid.Should().Equal(0);
        free.PointsGranted.Should().Be(10);
        free.HighestPaidLevelIndex.Should()
            .Be(0, "the premium stage must stay unpaid, not skipped");

        // Premium bought. The next signal settles what was owed.
        TaskProgressOutcome upgraded = TaskProgressRules.Set(
            task,
            free.HighestPaidLevelIndex,
            free.DistinctKeys,
            free.NewProgress,
            premiumUnlocked: true
        );

        upgraded.StagesPaid.Should().Equal(1);
        upgraded.PointsGranted.Should().Be(50);
    }

    [Fact]
    public void A_task_with_no_stages_never_moves()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Task() with
        {
            Levels = ImmutableArray<RewardTrackTaskLevelSnapshot>.Empty,
        };

        TaskProgressOutcome outcome = TaskProgressRules.Apply(task, 0, -1, "", 5, null, false);

        outcome.NewProgress.Should().Be(0);
        outcome.PointsGranted.Should().Be(0);
    }

    /// <summary>The wired action's entry point: a room writing a score rather than describing an act.</summary>
    [Fact]
    public void Set_writes_the_progress_and_pays_what_it_reaches()
    {
        TaskProgressOutcome outcome = TaskProgressRules.Set(ThreeStages, -1, "", 7, false);

        outcome.NewProgress.Should().Be(7);
        outcome.StagesPaid.Should().Equal(0, 1);
        outcome.PointsGranted.Should().Be(30);
    }

    [Fact]
    public void Set_clamps_above_the_last_stage()
    {
        TaskProgressRules.Set(ThreeStages, -1, "", 9999, false).NewProgress.Should().Be(20);
    }
}
