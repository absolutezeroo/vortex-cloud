using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record UpsertRewardTrackPrizeRequest(
    int TrackRowId,
    string PrizeId,
    int RequiredPoints,
    bool Premium,
    int SortOrder,
    IReadOnlyList<RewardTrackRewardBody> Rewards,
    string Reason
) : IReasonedRequest;
