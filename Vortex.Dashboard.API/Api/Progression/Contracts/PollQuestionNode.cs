using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// A question and its follow-ups. The shape is recursive because the client's branching walk is:
/// answering a root question can reveal a child.
/// </summary>
public sealed record PollQuestionNode(
    int Id,
    int SortOrder,
    int QuestionType,
    string QuestionTypeName,
    string QuestionText,
    int QuestionCategory,
    int QuestionAnswerType,
    int AnswerCount,
    IReadOnlyList<PollChoiceDetail> Choices,
    IReadOnlyList<PollQuestionNode> Children
);
