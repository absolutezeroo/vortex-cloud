using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeletePrizePoolRequest(int PoolId, string Reason) : IReasonedRequest;
