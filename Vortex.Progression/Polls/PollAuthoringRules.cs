using System;
using System.Linq;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Polls.Admin;

namespace Vortex.Progression.Polls;

/// <summary>
/// What makes a survey and a question well-formed, as pure decisions returning the error code the
/// dashboard shows. Extracted from <see cref="PollAdminService"/> so the rules are testable without
/// a database — most of them exist because the client is unforgiving about them, and a survey that
/// breaks one is not rejected by the client, it simply renders wrong.
/// </summary>
public static class PollAuthoringRules
{
    /// <summary>Null when the survey may be saved; otherwise the error code.</summary>
    public static string? ValidatePoll(PollSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Code))
        {
            return "poll_code_required";
        }

        if (string.IsNullOrWhiteSpace(spec.Headline))
        {
            return "poll_headline_required";
        }

        return string.IsNullOrWhiteSpace(spec.Summary) ? "poll_summary_required" : null;
    }

    /// <summary>Null when the question may be saved; otherwise the error code.</summary>
    public static string? ValidateQuestion(PollQuestionSpec spec)
    {
        if (spec.PollId <= 0)
        {
            return "poll_not_found";
        }

        if (string.IsNullOrWhiteSpace(spec.QuestionText))
        {
            return "question_text_required";
        }

        if (!Enum.IsDefined(spec.QuestionType))
        {
            return "question_type_invalid";
        }

        // Rating and Binary are in the client's enum but its survey dialog skips them outright, so a
        // question built on one is invisible to the player -- it would silently shorten the survey.
        if (spec.QuestionType is PollQuestionType.Rating or PollQuestionType.Binary)
        {
            return "question_type_unsupported";
        }

        if (!TakesChoices(spec.QuestionType))
        {
            return null;
        }

        if (spec.Choices.Count == 0)
        {
            return "question_choices_required";
        }

        if (spec.Choices.Any(c => string.IsNullOrWhiteSpace(c.Value)))
        {
            return "choice_value_required";
        }

        if (spec.Choices.Any(c => string.IsNullOrWhiteSpace(c.ChoiceText)))
        {
            return "choice_text_required";
        }

        // The answer a player sends back is the choice value; two identical values make the results
        // impossible to tell apart.
        return
            spec.Choices.Select(c => c.Value.Trim()).Distinct(StringComparer.Ordinal).Count()
            == spec.Choices.Count
            ? null
            : "choice_value_duplicate";
    }

    /// <summary>
    /// True when the question type carries a choice list. The client reads choices only for these
    /// two, so a text question's choices would be written and never read.
    /// </summary>
    public static bool TakesChoices(PollQuestionType questionType) =>
        questionType is PollQuestionType.SingleChoice or PollQuestionType.MultipleChoice;
}
