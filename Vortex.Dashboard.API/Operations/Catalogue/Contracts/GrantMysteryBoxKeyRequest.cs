using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record GrantMysteryBoxKeyRequest(int PlayerId, string Color, string Reason)
    : IReasonedRequest;
