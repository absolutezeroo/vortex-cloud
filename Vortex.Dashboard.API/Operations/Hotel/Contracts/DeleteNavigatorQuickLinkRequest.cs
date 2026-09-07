using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record DeleteNavigatorQuickLinkRequest(int QuickLinkId, string Reason)
    : IReasonedRequest;
