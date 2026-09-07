using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One question's answers: a per-choice tally for choice questions, the text players typed for the
/// others. Exactly one of the two is ever populated.
/// </summary>
/// <param name="Respondents">Distinct players, not answers — a checkbox question stores one row per
/// picked value, so the two differ.</param>
/// <param name="FreeTextTruncated">True when more text answers exist than the page shows.</param>
public sealed record PollQuestionResult(
    int Id,
    string QuestionText,
    int QuestionType,
    string QuestionTypeName,
    bool IsFollowUp,
    int? ParentQuestionId,
    int QuestionCategory,
    int Respondents,
    int AnswerCount,
    IReadOnlyList<PollTallyEntry> Tally,
    IReadOnlyList<PollFreeTextAnswer> FreeText,
    bool FreeTextTruncated
);
