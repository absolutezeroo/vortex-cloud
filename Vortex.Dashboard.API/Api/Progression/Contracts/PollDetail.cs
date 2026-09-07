using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One survey with its full question tree, which is what the editor loads.</summary>
public sealed record PollDetail(
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
    int SortOrder,
    IReadOnlyList<PollQuestionNode> Questions
);
