using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

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
) : IReasonedRequest;
