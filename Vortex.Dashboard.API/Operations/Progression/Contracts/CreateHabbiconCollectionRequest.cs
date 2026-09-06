using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record CreateHabbiconCollectionRequest(
    string Code,
    int SortOrder,
    bool Enabled,
    bool Hidden,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    int PriceCredits,
    int PriceActivityPoints,
    int ActivityPointType,
    string CampaignCode,
    string Reason
) : IReasonedRequest;
