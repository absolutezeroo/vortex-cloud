using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

public sealed record UpdateFishingRodTierRequest(
    int TierId,
    int Quality,
    int XpThreshold,
    string NameKey,
    int HandItemId,
    int CatchMultiplier,
    int GoldenMultiplier,
    int HookHavocChance,
    string Reason
) : IReasonedRequest;
