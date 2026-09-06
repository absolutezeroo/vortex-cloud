using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteNavigatorQuickLinkRequest(int QuickLinkId, string Reason)
    : IReasonedRequest;
