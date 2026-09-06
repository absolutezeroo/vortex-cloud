using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record UpdateNavigatorEventCategoryRequest(
    int CategoryId,
    string Name,
    bool Visible,
    string Reason
) : IReasonedRequest;
