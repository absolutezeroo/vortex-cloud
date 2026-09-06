using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteNavigatorEventCategoryRequest(int CategoryId, string Reason)
    : IReasonedRequest;
