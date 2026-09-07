using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record UpdateNavigatorContextRequest(
    int ContextId,
    string SearchCode,
    bool Visible,
    int QueryType,
    int OrderNum,
    string Reason
) : IReasonedRequest;
