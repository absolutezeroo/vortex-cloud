using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Everything a create takes, plus the row id — and deliberately not the status. A track's
/// lifecycle moves through publish and archive, which validate; letting an update set it would be a
/// way past that.
/// </summary>
public sealed record UpdateRewardTrackRequest(
    int TrackRowId,
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
