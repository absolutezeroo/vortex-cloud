using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteNavigatorContextRequest(int ContextId, string Reason) : IReasonedRequest;
