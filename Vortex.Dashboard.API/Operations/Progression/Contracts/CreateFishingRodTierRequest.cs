using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record CreateFishingRodTierRequest(
    int Quality,
    int XpThreshold,
    string NameKey,
    int HandItemId,
    int CatchMultiplier,
    int GoldenMultiplier,
    int HookHavocChance,
    string Reason
) : IReasonedRequest;
