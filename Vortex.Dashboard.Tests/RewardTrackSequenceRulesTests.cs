using FluentAssertions;
using Vortex.Dashboard.API.Admin.Rules;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Admin;
using Xunit;

namespace Vortex.Dashboard.Tests;

/// <summary>
/// What the sequence editor refuses before it is ever saved.
/// </summary>
/// <remarks>
/// These live with the dashboard because authoring is the dashboard's: the emulator walks sequences,
/// it does not judge them. The rules are checked against the REAL vocabulary the loaded translators
/// declare rather than a hand-written one, so a filter the editor accepts is one some action can
/// actually raise.
/// </remarks>
public sealed class RewardTrackSequenceRulesTests
{
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
            .FirstProblem(
                [new RewardTrackTaskStepSpec(action, [new(factKey, op, value)])],
                SignalVocabularyFixture.Real
            )
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void The_editor_refuses_a_reference_to_a_step_that_never_recorded_that_fact()
    {
        // "The same furniture" only works between two steps that both talk about furniture. Chat
        // emits no item, so $0 could never resolve.
        RewardTrackSequenceRules
            .FirstProblem(
                [
                    new RewardTrackTaskStepSpec(RewardTrackActions.ChatWithSomeone, []),
                    new RewardTrackTaskStepSpec(
                        RewardTrackActions.WalkOnFurni,
                        [new(RewardTrackFacts.Item, StepFilterOperator.Equals, "$0")]
                    ),
                ],
                SignalVocabularyFixture.Real
            )
            .Should()
            .Be("filter_reference_fact_not_captured");
    }

    [Fact]
    public void The_editor_accepts_the_operators_own_example()
    {
        RewardTrackSequenceRules
            .FirstProblem(
                [
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
                ],
                SignalVocabularyFixture.Real
            )
            .Should()
            .BeNull();
    }
}
