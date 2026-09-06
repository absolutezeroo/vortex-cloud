using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record MintableItemTypeRequest(
    int TypeId,
    string ProductCode,
    int StampPrice,
    DateTime StartsAt,
    DateTime EndsAt,
    bool RegionLocked,
    bool LimitedEdition,
    int EditionSize,
    bool Enabled,
    int SortOrder,
    string Reason
) : IReasonedRequest;
