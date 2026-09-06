using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record UpdatePollQuestionRequest(
    int QuestionId,
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
