using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Catalogue.Contracts;

public sealed record DeleteMysteryBoxPrizeRequest(int PrizeId, string Reason) : IReasonedRequest;
