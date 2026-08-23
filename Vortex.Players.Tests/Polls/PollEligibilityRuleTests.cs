using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Polls.Snapshots;
using Vortex.Progression.Grains;
using Vortex.Progression.Polls;
using Xunit;

namespace Vortex.Players.Tests.Polls;

/// <summary>
///     The decisions behind the poll grain: who gets offered a survey, who may answer it, and when
///     it counts as finished. Failure paths first — an offer that repeats itself after a player
///     declined is the regression that would actually annoy people.
/// </summary>
public sealed class PollEligibilityRuleTests
{
    private const int RoomId = 100;

    [Fact]
    public void CanOffer_IsFalse_WhenThePlayerAlreadyDeclinedIt()
    {
        PollEligibilityRule
            .CanOffer(Poll(), RoomId, PollParticipationState.Rejected)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CanOffer_IsFalse_WhenThePlayerAlreadyCompletedIt()
    {
        PollEligibilityRule
            .CanOffer(Poll(), RoomId, PollParticipationState.Completed)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CanOffer_IsFalse_WhenAnOfferIsAlreadyPending()
    {
        // Re-offering on every room entry would reopen the dialog under the player each time they
        // walked through a door.
        PollEligibilityRule
            .CanOffer(Poll(), RoomId, PollParticipationState.Offered)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CanOffer_IsFalse_ForAnotherRoom_WhenThePollIsPinned()
    {
        PollEligibilityRule
            .CanOffer(Poll(roomId: 999), RoomId, existingState: null)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CanOffer_IsFalse_WhenThePollHasNoQuestions()
    {
        // Accepting would open a dialog with nothing in it and no way to finish.
        PollDefinitionSnapshot empty = Poll() with
        {
            Questions = ImmutableArray<PollQuestionSnapshot>.Empty,
        };

        PollEligibilityRule.CanOffer(empty, RoomId, existingState: null).Should().BeFalse();
    }

    [Fact]
    public void CanOffer_IsFalse_WhenTheSurveyIsNotMeantForRoomEntry()
    {
        PollEligibilityRule
            .CanOffer(Poll() with { OfferOnRoomEntry = false }, RoomId, existingState: null)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CanOffer_IsTrue_ForAnUnseenHotelWidePoll()
    {
        PollEligibilityRule.CanOffer(Poll(), RoomId, existingState: null).Should().BeTrue();
    }

    [Fact]
    public void CanOffer_IsTrue_WhenThePollIsPinnedToThisRoom()
    {
        PollEligibilityRule
            .CanOffer(Poll(roomId: RoomId), RoomId, existingState: null)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData(null, true)] // never seen -- a widget reopened out of band
    [InlineData(PollParticipationState.Offered, true)]
    [InlineData(PollParticipationState.Started, true)] // resuming is fine
    [InlineData(PollParticipationState.Rejected, false)]
    [InlineData(PollParticipationState.Completed, false)]
    public void CanStart_DependsOnTheRecordedState(PollParticipationState? state, bool expected)
    {
        PollEligibilityRule.CanStart(state).Should().Be(expected);
    }

    [Fact]
    public void OwnsQuestion_IsFalse_ForAQuestionOfAnotherPoll()
    {
        // The client sends a poll id and a question id side by side; nothing stops a crafted packet
        // pairing one survey with another's question.
        PollEligibilityRule.OwnsQuestion(Poll(), questionId: 404).Should().BeFalse();
    }

    [Fact]
    public void OwnsQuestion_IsTrue_ForAFollowUpQuestion()
    {
        // Follow-ups are nested one level down and are answered like any other question.
        PollEligibilityRule.OwnsQuestion(Poll(), questionId: 2).Should().BeTrue();
    }

    [Fact]
    public void IsComplete_IsFalse_WhileARootQuestionIsUnanswered()
    {
        PollEligibilityRule.IsComplete(Poll(), Answered(1)).Should().BeFalse();
    }

    [Fact]
    public void IsComplete_IsFalse_ForAPollWithNoQuestions()
    {
        PollDefinitionSnapshot empty = Poll() with
        {
            Questions = ImmutableArray<PollQuestionSnapshot>.Empty,
        };

        PollEligibilityRule.IsComplete(empty, Answered()).Should().BeFalse();
    }

    [Fact]
    public void IsComplete_IsTrue_WhenEveryRootIsAnswered_EvenWithNoFollowUpAnswer()
    {
        // Which follow-up a player sees depends on the choices they made, so requiring one would
        // leave most NPS surveys permanently unfinished.
        PollEligibilityRule.IsComplete(Poll(), Answered(1, 3)).Should().BeTrue();
    }

    [Fact]
    public void IsComplete_IgnoresAnswersToQuestionsOfOtherPolls()
    {
        PollEligibilityRule.IsComplete(Poll(), Answered(1, 999)).Should().BeFalse();
    }

    private static HashSet<int> Answered(params int[] questionIds) => [.. questionIds];

    /// <summary>Two root questions, the first carrying one NPS follow-up (question 2).</summary>
    private static PollDefinitionSnapshot Poll(int? roomId = null) =>
        new()
        {
            Id = 1,
            Code = "hotel_satisfaction",
            PollType = "nps",
            Headline = "Got a minute?",
            Summary = "Three questions.",
            StartMessage = "Here we go.",
            EndMessage = "Thanks!",
            NpsPoll = true,
            OfferOnRoomEntry = true,
            RoomId = roomId,
            SortOrder = 0,
            Questions =
            [
                new PollQuestionSnapshot
                {
                    Id = 1,
                    SortOrder = 0,
                    QuestionType = PollQuestionType.SingleChoice,
                    QuestionText = "Would you recommend us?",
                    QuestionCategory = 0,
                    QuestionAnswerType = 0,
                    Children =
                    [
                        new PollQuestionSnapshot
                        {
                            Id = 2,
                            SortOrder = 0,
                            QuestionType = PollQuestionType.TextArea,
                            QuestionText = "What let you down?",
                            QuestionCategory = 3,
                            QuestionAnswerType = 0,
                        },
                    ],
                },
                new PollQuestionSnapshot
                {
                    Id = 3,
                    SortOrder = 1,
                    QuestionType = PollQuestionType.TextLine,
                    QuestionText = "Anything else?",
                    QuestionCategory = 0,
                    QuestionAnswerType = 0,
                },
            ],
        };
}
