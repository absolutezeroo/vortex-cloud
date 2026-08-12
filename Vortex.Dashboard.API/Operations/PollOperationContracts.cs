using System.Collections.Generic;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for the dashboard's poll admin operations, each carrying a mandatory audited
/// <c>Reason</c>. <c>NpsPoll</c> turns on the client's branching walk, without which a follow-up
/// question is never shown; <c>RoomId</c> pins the offer to one room and null offers it anywhere.
/// </summary>
public sealed record CreatePollRequest(
    string Code,
    string PollType,
    string Headline,
    string Summary,
    string StartMessage,
    string EndMessage,
    bool NpsPoll,
    bool Enabled,
    bool OfferOnRoomEntry,
    int? RoomId,
    int SortOrder,
    string Reason
);

public sealed record UpdatePollRequest(
    int PollId,
    string Code,
    string PollType,
    string Headline,
    string Summary,
    string StartMessage,
    string EndMessage,
    bool NpsPoll,
    bool Enabled,
    bool OfferOnRoomEntry,
    int? RoomId,
    int SortOrder,
    string Reason
);

public sealed record DeletePollRequest(int PollId, string Reason);

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
);

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
);

public sealed record DeletePollQuestionRequest(int QuestionId, string Reason);

/// <summary>
/// One selectable answer. <c>Value</c> is what the client sends back and what the results are keyed
/// on; <c>ChoiceType</c> is the NPS branch key (0 = picking it leads to no follow-up).
/// </summary>
public sealed record PollChoiceBody(string Value, string ChoiceText, int ChoiceType, int SortOrder);
