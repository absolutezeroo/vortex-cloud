using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record CollectionRequest(
    int CollectionId,
    string CollectionCode,
    string Name,
    int BoostScore,
    int Status,
    string? RewardProductCode,
    string? BonusProductCode,
    string Reason
) : IReasonedRequest;
