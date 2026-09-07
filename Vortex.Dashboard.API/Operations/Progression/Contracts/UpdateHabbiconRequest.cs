using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

public sealed record UpdateHabbiconRequest(
    int HabbiconId,
    string Code,
    int CollectionId,
    int SortOrder,
    bool IsCollectionReward,
    int PriceCredits,
    int PriceActivityPoints,
    int ActivityPointType,
    bool Enabled,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    string Reason
) : IReasonedRequest;
