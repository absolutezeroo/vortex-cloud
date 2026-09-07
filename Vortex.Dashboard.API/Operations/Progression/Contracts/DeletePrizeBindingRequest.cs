using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

public sealed record DeletePrizeBindingRequest(int BindingId, string Reason) : IReasonedRequest;
