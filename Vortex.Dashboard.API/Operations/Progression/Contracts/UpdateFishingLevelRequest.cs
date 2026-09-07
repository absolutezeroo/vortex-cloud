using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

public sealed record UpdateFishingLevelRequest(
    int LevelId,
    int Level,
    int XpThreshold,
    string Reason
) : IReasonedRequest;
