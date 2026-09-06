using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record CreateFishingLevelRequest(int Level, int XpThreshold, string Reason)
    : IReasonedRequest;
