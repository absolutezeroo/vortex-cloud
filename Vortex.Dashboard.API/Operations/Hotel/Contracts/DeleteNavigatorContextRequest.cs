using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record DeleteNavigatorContextRequest(int ContextId, string Reason) : IReasonedRequest;
