using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record CreateRewardTrackRequest(
    string TrackId,
    string Theme,
    int SortOrder,
    DateTime? StartsAt,
    DateTime? ProgressEndsAt,
    DateTime? ClaimEndsAt,
    int UnlockKind,
    string UnlockValue,
    int CompletionPolicy,
    bool PremiumEnabled,
    int PremiumBoostPerMille,
    int PremiumInstantPoints,
    int PremiumCostCredits,
    int PremiumCostDiamonds,
    bool Hidden,
    string CampaignCode,
    string Reason
) : IReasonedRequest;
