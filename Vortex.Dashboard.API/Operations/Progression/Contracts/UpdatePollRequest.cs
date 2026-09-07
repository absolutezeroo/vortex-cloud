using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

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
) : IReasonedRequest;
