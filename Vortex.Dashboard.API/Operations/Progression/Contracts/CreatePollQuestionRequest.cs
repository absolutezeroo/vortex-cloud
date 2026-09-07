using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

/// <summary>
/// A question and its full choice list. <c>Choices</c> replaces whatever the question had: answers
/// store the picked value as text, not a choice id, so retiring a choice never destroys results.
/// <c>ParentQuestionId</c> makes this an NPS follow-up of a root question, shown only when the
/// parent's picked choice carries a <c>ChoiceType</c> equal to this question's <c>QuestionCategory</c>.
/// </summary>
public sealed record CreatePollQuestionRequest(
    int PollId,
    int? ParentQuestionId,
    int SortOrder,
    int QuestionType,
    string QuestionText,
    int QuestionCategory,
    int QuestionAnswerType,
    IReadOnlyList<PollChoiceBody> Choices,
    string Reason
) : IReasonedRequest;
