using FluentAssertions;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Admin;
using Vortex.Primitives.RewardTracks.Snapshots;
using Vortex.RewardTracks.Progression;
using Xunit;

namespace Vortex.Rewards.Tests;

/// <summary>
/// Task conditions: the composable half of what a task counts.
/// </summary>
/// <remarks>
/// Every failure here is silent in production. A condition that cannot match does not error — the
/// task simply never advances, and an operator cannot tell that apart from "nobody has done it
/// yet". So both halves are asserted: that a well-formed condition filters exactly what it says,
/// and that an ill-formed one is refused at the form instead of shipping as a dead task.
/// </remarks>
public sealed class TaskConditionTests
{
    private static TaskProgressOutcome Apply(
        RewardTrackTaskDefinitionSnapshot task,
        int amount,
        string? target
    ) => TaskProgressRules.Apply(task, 0, -1, string.Empty, amount, target, premiumUnlocked: false);

    [Fact]
    public void TaskWithoutConditionsCountsEverythingItDidBefore()
    {
        // The default has to be untouched: 16 seeded tasks and every existing campaign carry no
        // conditions, and a change in this line would silently rewrite all of them.
        Apply(Content.Task(), 1, "anything").NewProgress.Should().Be(1);
    }

    [Theory]
    [InlineData("4312", true)]
    [InlineData("4313", false)]
    public void TargetEqualsFiltersOnTheSignalsTarget(string target, bool counts)
    {
        RewardTrackTaskDefinitionSnapshot task = Content.TaskWith(
            conditions: Content.Condition(
                TaskConditionField.Target,
                TaskConditionOperator.Equals,
                "4312"
            )
        );

        Apply(task, 1, target).NewProgress.Should().Be(counts ? 1 : 0);
    }

    [Theory]
    [InlineData("4312", true)]
    [InlineData("4313", true)]
    [InlineData("9999", false)]
    public void OneOfAcceptsAnyEntryInTheList(string target, bool counts)
    {
        // The operator that earns the feature: four sofas used to be four separate tasks. The
        // spacing is deliberate -- an operator types "4312, 4313" and means two ids.
        RewardTrackTaskDefinitionSnapshot task = Content.TaskWith(
            conditions: Content.Condition(
                TaskConditionField.Target,
                TaskConditionOperator.OneOf,
                "4312, 4313"
            )
        );

        Apply(task, 1, target).NewProgress.Should().Be(counts ? 1 : 0);
    }

    [Fact]
    public void ATargetConditionRejectsASignalThatNamesNothing()
    {
        // Including NotEquals. "Any room but the lounge" is a claim about a room, and an action
        // carrying no room has not made it true -- letting it through would quietly count every
        // targetless action in the hotel.
        RewardTrackTaskDefinitionSnapshot task = Content.TaskWith(
            conditions: Content.Condition(
                TaskConditionField.Target,
                TaskConditionOperator.NotEquals,
                "7"
            )
        );

        Apply(task, 1, null).NewProgress.Should().Be(0);
    }

    [Theory]
    [InlineData(4, 0)]
    [InlineData(5, 5)]
    [InlineData(9, 9)]
    public void AmountAtLeastGatesOnHowMuchTheSignalReported(int amount, int expected)
    {
        // Counter mode adds the amount, so a bulk purchase of five counts five -- the condition
        // decides whether it counts at all, not how much.
        RewardTrackTaskDefinitionSnapshot task = Content.TaskWith(
            conditions: Content.Condition(
                TaskConditionField.Amount,
                TaskConditionOperator.AtLeast,
                "5"
            )
        );

        Apply(task with { Levels = [Content.Level(0, 100, 10)] }, amount, "x")
            .NewProgress.Should()
            .Be(expected);
    }

    [Fact]
    public void EveryConditionMustHold()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.TaskWith(
            conditions:
            [
                Content.Condition(TaskConditionField.Target, TaskConditionOperator.Equals, "4312"),
                Content.Condition(TaskConditionField.Amount, TaskConditionOperator.AtLeast, "2"),
            ]
        );

        Apply(task with { Levels = [Content.Level(0, 100, 10)] }, 3, "4312")
            .NewProgress.Should()
            .Be(3);
        Apply(task with { Levels = [Content.Level(0, 100, 10)] }, 1, "4312")
            .NewProgress.Should()
            .Be(0);
        Apply(task with { Levels = [Content.Level(0, 100, 10)] }, 3, "4313")
            .NewProgress.Should()
            .Be(0);
    }

    [Fact]
    public void ConditionsAreAndedWithTheParameterNotInsteadOfIt()
    {
        // The parameter is on the wire and the client reads it. A task carrying both has to honour
        // both, or content written before conditions existed would start counting more.
        RewardTrackTaskDefinitionSnapshot task = Content.TaskWith(
            parameter: "4312",
            conditions: Content.Condition(
                TaskConditionField.Amount,
                TaskConditionOperator.AtLeast,
                "1"
            )
        );

        Apply(task, 1, "4312").NewProgress.Should().Be(1);
        Apply(task, 1, "4313").NewProgress.Should().Be(0);
    }

    [Fact]
    public void AnUnparseableNumberFailsTheConditionRatherThanThrowing()
    {
        // This runs on the room's event path behind content someone typed. A campaign that throws
        // here would take the pipeline down; a task that never advances is recoverable.
        RewardTrackTaskDefinitionSnapshot task = Content.TaskWith(
            conditions: Content.Condition(
                TaskConditionField.Amount,
                TaskConditionOperator.AtLeast,
                "not a number"
            )
        );

        Apply(task, 10, "x").NewProgress.Should().Be(0);
    }

    [Theory]
    [InlineData(TaskConditionField.Target, TaskConditionOperator.AtLeast, "5")]
    [InlineData(TaskConditionField.Amount, TaskConditionOperator.OneOf, "1, 2")]
    [InlineData(TaskConditionField.Target, TaskConditionOperator.Equals, "")]
    [InlineData(TaskConditionField.Amount, TaskConditionOperator.AtLeast, "ten")]
    [InlineData(TaskConditionField.Target, TaskConditionOperator.OneOf, "4312")]
    public void TheFormRefusesAConditionThatCannotWork(
        TaskConditionField field,
        TaskConditionOperator op,
        string value
    ) =>
        RewardTrackConditionRules
            .FirstProblem([new RewardTrackTaskConditionSpec(field, op, value)])
            .Should()
            .NotBeNull();

    [Fact]
    public void TheFormAcceptsTheOnesThatDo() =>
        RewardTrackConditionRules
            .FirstProblem([
                new RewardTrackTaskConditionSpec(
                    TaskConditionField.Target,
                    TaskConditionOperator.OneOf,
                    "4312, 4313"
                ),
                new RewardTrackTaskConditionSpec(
                    TaskConditionField.Amount,
                    TaskConditionOperator.AtMost,
                    "10"
                ),
            ])
            .Should()
            .BeNull();
}
