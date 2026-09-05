using System.Collections.Immutable;
using FluentAssertions;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Admin;
using Vortex.Primitives.RewardTracks.Snapshots;
using Vortex.RewardTracks.Progression;
using Xunit;

namespace Vortex.Rewards.Tests;

/// <summary>
/// Task sequences: several actions, in order, with later steps able to point back at what an
/// earlier one matched.
/// </summary>
/// <remarks>
/// The two shapes an operator actually asked for are both here as end-to-end walks — "place a floor
/// item, walk on it, pick it up" and "enter a room, add a friend, message them" — because the parts
/// can each be right while the sequence as a whole is not, and because a wrong one fails silently:
/// the task simply never advances.
/// </remarks>
public sealed class TaskSequenceTests
{
    /// <summary>Walks a whole signal through a task, carrying the cursor and captures forward.</summary>
    private sealed class Walker(RewardTrackTaskDefinitionSnapshot task)
    {
        public int Progress { get; private set; }
        public int Step { get; private set; }
        public string Captures { get; private set; } = string.Empty;
        private int _watermark = -1;
        private string _distinct = string.Empty;

        public Walker Send(string actionCode, params RewardTrackFactSnapshot[] facts)
        {
            TaskProgressOutcome outcome = TaskProgressRules.Apply(
                task,
                Progress,
                _watermark,
                _distinct,
                Step,
                Captures,
                actionCode,
                1,
                null,
                [.. facts],
                premiumUnlocked: false
            );

            Progress = outcome.NewProgress;
            Step = outcome.NewStep;
            Captures = outcome.Captures;
            _watermark = outcome.HighestPaidLevelIndex;
            _distinct = outcome.DistinctKeys;

            return this;
        }
    }

    private static RewardTrackFactSnapshot Fact(string key, string value) => new(key, value);

    [Fact]
    public void A_plain_task_is_a_sequence_of_one()
    {
        // The default has to be untouched. Every campaign written before sequences existed is a
        // one-step task, and a change here would silently rewrite all of them.
        new Walker(Content.Task(actionCode: "act"))
            .Send("act")
            .Progress.Should()
            .Be(1);
    }

    [Fact]
    public void Steps_only_count_in_order()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Sequence(
            steps:
            [
                Content.Step(RewardTrackActions.ChatWithSomeone),
                Content.Step(RewardTrackActions.RequestFriend),
            ]
        );

        // The second action first: nothing, because the player is standing on step 0.
        new Walker(task)
            .Send(RewardTrackActions.RequestFriend)
            .Progress.Should()
            .Be(0);

        Walker walker = new Walker(task).Send(RewardTrackActions.ChatWithSomeone);
        walker.Progress.Should().Be(0, "the sequence is not finished yet");
        walker.Step.Should().Be(1, "but the cursor moved");

        walker.Send(RewardTrackActions.RequestFriend).Progress.Should().Be(1);
        walker.Step.Should().Be(0, "a finished sequence starts again");
    }

    [Fact]
    public void An_unrelated_action_does_not_reset_the_sequence()
    {
        // "Talk, then add a friend" has to survive the fifty other things a player does in between.
        // Resetting would make every multi-step task read as broken.
        RewardTrackTaskDefinitionSnapshot task = Content.Sequence(
            steps:
            [
                Content.Step(RewardTrackActions.ChatWithSomeone),
                Content.Step(RewardTrackActions.RequestFriend),
            ]
        );

        new Walker(task)
            .Send(RewardTrackActions.ChatWithSomeone)
            .Send(RewardTrackActions.Dance)
            .Send(RewardTrackActions.PlaceItem)
            .Send(RewardTrackActions.RequestFriend)
            .Progress.Should()
            .Be(1);
    }

    [Fact]
    public void Place_a_floor_item_walk_on_it_then_pick_it_up()
    {
        // The operator's own example. Steps 2 and 3 name the item step 1 matched, which is the
        // whole point: without the back-reference this would count walking on anything.
        RewardTrackTaskDefinitionSnapshot task = Content.Sequence(
            steps:
            [
                Content.Step(
                    RewardTrackActions.PlaceItem,
                    Content.Filter(
                        RewardTrackFacts.Placement,
                        StepFilterOperator.Equals,
                        RewardTrackFacts.PlacementFloor
                    )
                ),
                Content.Step(
                    RewardTrackActions.WalkOnFurni,
                    Content.Filter(RewardTrackFacts.Item, StepFilterOperator.Equals, "$0")
                ),
                Content.Step(
                    RewardTrackActions.PickUpItem,
                    Content.Filter(RewardTrackFacts.Item, StepFilterOperator.Equals, "$0")
                ),
            ]
        );

        new Walker(task)
            .Send(
                RewardTrackActions.PlaceItem,
                Fact(RewardTrackFacts.Item, "500"),
                Fact(RewardTrackFacts.Placement, RewardTrackFacts.PlacementFloor)
            )
            .Send(RewardTrackActions.WalkOnFurni, Fact(RewardTrackFacts.Item, "500"))
            .Send(RewardTrackActions.PickUpItem, Fact(RewardTrackFacts.Item, "500"))
            .Progress.Should()
            .Be(1);
    }

    [Fact]
    public void Walking_on_a_different_item_does_not_advance_the_sequence()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Sequence(
            steps:
            [
                Content.Step(RewardTrackActions.PlaceItem),
                Content.Step(
                    RewardTrackActions.WalkOnFurni,
                    Content.Filter(RewardTrackFacts.Item, StepFilterOperator.Equals, "$0")
                ),
            ]
        );

        Walker walker = new Walker(task)
            .Send(RewardTrackActions.PlaceItem, Fact(RewardTrackFacts.Item, "500"))
            .Send(RewardTrackActions.WalkOnFurni, Fact(RewardTrackFacts.Item, "999"));

        walker.Progress.Should().Be(0);
        walker.Step.Should().Be(1, "the player is still waiting to walk on the right one");

        walker.Send(RewardTrackActions.WalkOnFurni, Fact(RewardTrackFacts.Item, "500"));
        walker.Progress.Should().Be(1);
    }

    [Fact]
    public void A_wall_item_does_not_satisfy_a_floor_step()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Sequence(
            steps:
            [
                Content.Step(
                    RewardTrackActions.PlaceItem,
                    Content.Filter(
                        RewardTrackFacts.Placement,
                        StepFilterOperator.Equals,
                        RewardTrackFacts.PlacementFloor
                    )
                ),
            ]
        );

        new Walker(task)
            .Send(
                RewardTrackActions.PlaceItem,
                Fact(RewardTrackFacts.Placement, RewardTrackFacts.PlacementWall)
            )
            .Progress.Should()
            .Be(0);
    }

    [Fact]
    public void Enter_a_room_add_a_friend_then_message_that_same_friend()
    {
        // The operator's second example, minus its last step: "join their flat" needs the room's
        // owner on the entry event, which nothing carries yet.
        RewardTrackTaskDefinitionSnapshot task = Content.Sequence(
            steps:
            [
                Content.Step(RewardTrackActions.EnterOtherUsersRoom),
                Content.Step(RewardTrackActions.RequestFriend),
                Content.Step(
                    RewardTrackActions.SendMessengerMessage,
                    Content.Filter(RewardTrackFacts.Player, StepFilterOperator.Equals, "$1")
                ),
            ]
        );

        Walker walker = new Walker(task)
            .Send(RewardTrackActions.EnterOtherUsersRoom, Fact(RewardTrackFacts.Room, "7"))
            .Send(RewardTrackActions.RequestFriend, Fact(RewardTrackFacts.Player, "42"));

        // Messaging somebody else is not the friend they just added.
        walker.Send(RewardTrackActions.SendMessengerMessage, Fact(RewardTrackFacts.Player, "99"));
        walker.Progress.Should().Be(0);

        walker.Send(RewardTrackActions.SendMessengerMessage, Fact(RewardTrackFacts.Player, "42"));
        walker.Progress.Should().Be(1);
    }

    [Fact]
    public void A_signal_missing_the_fact_a_step_asks_about_does_not_satisfy_it()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Sequence(
            steps:
            [
                Content.Step(
                    RewardTrackActions.PlaceItem,
                    Content.Filter(RewardTrackFacts.Item, StepFilterOperator.NotEquals, "500")
                ),
            ]
        );

        // NotEquals included: "anything but that sofa" is a claim about a sofa, and a signal naming
        // no item has not made it true.
        new Walker(task)
            .Send(RewardTrackActions.PlaceItem)
            .Progress.Should()
            .Be(0);
    }

    [Fact]
    public void The_captures_are_cleared_when_the_sequence_completes()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Sequence(
            steps:
            [
                Content.Step(RewardTrackActions.PlaceItem),
                Content.Step(RewardTrackActions.PickUpItem),
            ]
        );

        Walker walker = new Walker(task)
            .Send(RewardTrackActions.PlaceItem, Fact(RewardTrackFacts.Item, "500"))
            .Send(RewardTrackActions.PickUpItem, Fact(RewardTrackFacts.Item, "500"));

        // A second run must not resolve $0 against the first run's furniture.
        walker.Captures.Should().BeEmpty();
        walker.Step.Should().Be(0);
    }

    [Theory]
    // A fact the step's own action never emits: nothing would ever satisfy it.
    [InlineData(RewardTrackActions.PlaceItem, RewardTrackFacts.Player, "42")]
    // A reference to a step that has not run when this one is tested.
    [InlineData(RewardTrackActions.PlaceItem, RewardTrackFacts.Item, "$0")]
    [InlineData(RewardTrackActions.PlaceItem, RewardTrackFacts.Item, "$5")]
    // A list with one entry is Equals wearing a hat, and usually a wrong separator.
    [InlineData(RewardTrackActions.PlaceItem, RewardTrackFacts.Item, "500")]
    public void The_editor_refuses_a_filter_that_cannot_work(
        string action,
        string factKey,
        string value
    )
    {
        StepFilterOperator op =
            value == "500" ? StepFilterOperator.OneOf : StepFilterOperator.Equals;

        RewardTrackSequenceRules
            .FirstProblem([new RewardTrackTaskStepSpec(action, [new(factKey, op, value)])])
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void The_editor_refuses_a_reference_to_a_step_that_never_recorded_that_fact()
    {
        // "The same furniture" only works between two steps that both talk about furniture. Chat
        // emits no item, so $0 could never resolve.
        RewardTrackSequenceRules
            .FirstProblem([
                new RewardTrackTaskStepSpec(RewardTrackActions.ChatWithSomeone, []),
                new RewardTrackTaskStepSpec(
                    RewardTrackActions.WalkOnFurni,
                    [new(RewardTrackFacts.Item, StepFilterOperator.Equals, "$0")]
                ),
            ])
            .Should()
            .Be("filter_reference_fact_not_captured");
    }

    [Fact]
    public void The_editor_accepts_the_operators_own_example()
    {
        RewardTrackSequenceRules
            .FirstProblem([
                new RewardTrackTaskStepSpec(
                    RewardTrackActions.PlaceItem,
                    [
                        new(
                            RewardTrackFacts.Placement,
                            StepFilterOperator.Equals,
                            RewardTrackFacts.PlacementFloor
                        ),
                    ]
                ),
                new RewardTrackTaskStepSpec(
                    RewardTrackActions.WalkOnFurni,
                    [new(RewardTrackFacts.Item, StepFilterOperator.Equals, "$0")]
                ),
                new RewardTrackTaskStepSpec(
                    RewardTrackActions.PickUpItem,
                    [new(RewardTrackFacts.Item, StepFilterOperator.Equals, "$0")]
                ),
            ])
            .Should()
            .BeNull();
    }
}
