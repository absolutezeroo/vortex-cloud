using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record UpdateNavigatorEventCategoryRequest(
    int CategoryId,
    string Name,
    bool Visible,
    string Reason
) : IReasonedRequest;
