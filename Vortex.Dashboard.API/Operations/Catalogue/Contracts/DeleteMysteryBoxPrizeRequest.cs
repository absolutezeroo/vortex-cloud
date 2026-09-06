using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteMysteryBoxPrizeRequest(int PrizeId, string Reason) : IReasonedRequest;
