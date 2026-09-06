using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteFishingLevelRequest(int LevelId, string Reason) : IReasonedRequest;
