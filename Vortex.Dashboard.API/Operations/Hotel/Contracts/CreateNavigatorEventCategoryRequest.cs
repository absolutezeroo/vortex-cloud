using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record CreateNavigatorEventCategoryRequest(string Name, bool Visible, string Reason)
    : IReasonedRequest;
