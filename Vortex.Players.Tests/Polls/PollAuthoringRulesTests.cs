using System.Collections.Generic;
using FluentAssertions;
using Vortex.Players.Polls;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Polls.Admin;
using Xunit;

namespace Vortex.Players.Tests.Polls;

/// <summary>
///     What the dashboard is allowed to save. Every rule here exists because the client does not
///     complain about the mistake — it just renders the survey wrong, which is far harder to notice
///     than a rejected form.
/// </summary>
public sealed class PollAuthoringRulesTests
{
    [Theory]
    [InlineData(PollQuestionType.Rating)]
    [InlineData(PollQuestionType.Binary)]
    public void ValidateQuestion_RejectsTheTypesTheSurveyDialogSkips(PollQuestionType type)
    {
        // Both are in the client's poll enum, so they look legitimate in a picker. Its content
        // dialog switches on questionType - 1 and handles 0..3 only; anything else makes it jump
        // straight to the next question, silently shortening the survey.
        Question(type, Choice("a", "A")).Validate().Should().Be("question_type_unsupported");
    }

    [Fact]
    public void ValidateQuestion_RejectsTwoChoicesSharingAValue()
    {
        // The answer that comes back is the value, not the choice id -- duplicates would collapse
        // into one bar in the results with no way to tell which choice was picked.
        Question(PollQuestionType.SingleChoice, Choice("yes", "Yes"), Choice("yes", "Yeah"))
            .Validate()
            .Should()
            .Be("choice_value_duplicate");
    }

    [Fact]
    public void ValidateQuestion_RejectsAChoiceQuestionWithNoChoices()
    {
        // The client would render an empty selector the player cannot answer, stalling the survey.
        Question(PollQuestionType.MultipleChoice)
            .Validate()
            .Should()
            .Be("question_choices_required");
    }

    [Theory]
    [InlineData("", "Label", "choice_value_required")]
    [InlineData("value", "  ", "choice_text_required")]
    public void ValidateQuestion_RejectsAHalfFilledChoice(
        string value,
        string text,
        string expected
    )
    {
        Question(PollQuestionType.SingleChoice, Choice(value, text))
            .Validate()
            .Should()
            .Be(expected);
    }

    [Fact]
    public void ValidateQuestion_AcceptsAFreeTextQuestionWithNoChoices()
    {
        Question(PollQuestionType.TextArea).Validate().Should().BeNull();
    }

    [Fact]
    public void ValidateQuestion_RejectsAnEmptyQuestionText()
    {
        PollQuestionSpec blank = Question(PollQuestionType.TextLine) with { QuestionText = "   " };

        blank.Validate().Should().Be("question_text_required");
    }

    [Fact]
    public void ValidateQuestion_AcceptsAWellFormedChoiceQuestion()
    {
        Question(
                PollQuestionType.SingleChoice,
                Choice("10", "Definitely"),
                Choice("0", "No chance")
            )
            .Validate()
            .Should()
            .BeNull();
    }

    [Theory]
    [InlineData(PollQuestionType.SingleChoice, true)]
    [InlineData(PollQuestionType.MultipleChoice, true)]
    [InlineData(PollQuestionType.TextLine, false)]
    [InlineData(PollQuestionType.TextArea, false)]
    public void TakesChoices_MatchesTheTypesTheClientReadsAChoiceListFor(
        PollQuestionType type,
        bool expected
    )
    {
        PollAuthoringRules.TakesChoices(type).Should().Be(expected);
    }

    [Theory]
    [InlineData("", "Headline", "Summary", "poll_code_required")]
    [InlineData("code", " ", "Summary", "poll_headline_required")]
    [InlineData("code", "Headline", "", "poll_summary_required")]
    [InlineData("code", "Headline", "Summary", null)]
    public void ValidatePoll_RequiresTheThreeFieldsTheOfferDialogShows(
        string code,
        string headline,
        string summary,
        string? expected
    )
    {
        PollAuthoringRules
            .ValidatePoll(
                new PollSpec(
                    code,
                    "nps",
                    headline,
                    summary,
                    StartMessage: string.Empty,
                    EndMessage: string.Empty,
                    NpsPoll: false,
                    Enabled: true,
                    OfferOnRoomEntry: true,
                    RoomId: null,
                    SortOrder: 0
                )
            )
            .Should()
            .Be(expected);
    }

    private static PollChoiceSpec Choice(string value, string text) =>
        new(value, text, ChoiceType: 0, SortOrder: 0);

    private static PollQuestionSpec Question(
        PollQuestionType type,
        params PollChoiceSpec[] choices
    ) =>
        new(
            PollId: 1,
            ParentQuestionId: null,
            SortOrder: 0,
            QuestionType: type,
            QuestionText: "Would you recommend us?",
            QuestionCategory: 0,
            QuestionAnswerType: 0,
            Choices: new List<PollChoiceSpec>(choices)
        );
}

file static class PollQuestionSpecExtensions
{
    public static string? Validate(this PollQuestionSpec spec) =>
        PollAuthoringRules.ValidateQuestion(spec);
}
