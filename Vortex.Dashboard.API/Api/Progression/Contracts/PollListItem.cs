namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One survey in the list, with its question counts and its offer-to-completion funnel.
/// </summary>
/// <param name="RoomName">Null when the poll is pinned to a room that no longer exists.</param>
/// <param name="RoomMissing">
/// True for exactly that case. A poll pinned to a deleted room never matches anyone, which is worth
/// seeing in the list rather than wondering why the offer stopped appearing.
/// </param>
/// <param name="Offerable">
/// Enabled and holding at least one root question. A survey with no root question is never offered
/// — the grain skips it — so "enabled" alone would tell an operator the wrong thing.
/// </param>
public sealed record PollListItem(
    int Id,
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
    string? RoomName,
    bool RoomMissing,
    int SortOrder,
    int RootQuestionCount,
    int FollowUpCount,
    int OfferedCount,
    int StartedCount,
    int CompletedCount,
    int RejectedCount,
    double CompletionRate,
    bool Offerable
);
