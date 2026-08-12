using System.Collections.Generic;

namespace Vortex.Primitives.Polls.Admin;

/// <summary>
/// Outcome of a poll admin write, mirroring the quest admin result. The poll admin service is a
/// plain in-process singleton (not a grain), so no Orleans attributes.
/// </summary>
public sealed record PollAdminResult(bool Success, int? Id, string? ErrorCode)
{
    public static PollAdminResult Ok(int id) => new(true, id, null);

    public static PollAdminResult Fail(string errorCode) => new(false, null, errorCode);
}

/// <summary>
/// Create/update spec for a survey. <paramref name="NpsPoll"/> enables the client's branching walk:
/// only then does a follow-up question ever get shown. <paramref name="RoomId"/> pins the offer to
/// one room; null offers it anywhere.
/// </summary>
public sealed record PollSpec(
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
    int SortOrder
);

/// <summary>
/// Create/update spec for one question. <paramref name="Choices"/> replaces the question's choice
/// list wholesale — safe because an answer stores the picked <see cref="PollChoiceSpec.Value"/>,
/// not a foreign key, so retiring a choice never destroys the answers that referenced it (they show
/// up in the results as retired).
/// </summary>
public sealed record PollQuestionSpec(
    int PollId,
    int? ParentQuestionId,
    int SortOrder,
    PollQuestionType QuestionType,
    string QuestionText,
    int QuestionCategory,
    int QuestionAnswerType,
    IReadOnlyList<PollChoiceSpec> Choices
);

public sealed record PollChoiceSpec(string Value, string ChoiceText, int ChoiceType, int SortOrder);
