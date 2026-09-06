using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteFishingRodTierRequest(int TierId, string Reason) : IReasonedRequest;
