using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteNavigatorFlatCategoryRequest(int CategoryId, string Reason)
    : IReasonedRequest;
