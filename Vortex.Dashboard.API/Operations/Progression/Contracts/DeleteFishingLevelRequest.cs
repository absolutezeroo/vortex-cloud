using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

public sealed record DeleteFishingLevelRequest(int LevelId, string Reason) : IReasonedRequest;
