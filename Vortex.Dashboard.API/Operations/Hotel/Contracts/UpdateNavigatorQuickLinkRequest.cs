using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record UpdateNavigatorQuickLinkRequest(
    int QuickLinkId,
    int ContextId,
    string SearchCode,
    string Filter,
    string Localization,
    int QueryType,
    int OrderNum,
    string Reason
) : IReasonedRequest;
