using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

public sealed record DeleteFishingRodTierRequest(int TierId, string Reason) : IReasonedRequest;
