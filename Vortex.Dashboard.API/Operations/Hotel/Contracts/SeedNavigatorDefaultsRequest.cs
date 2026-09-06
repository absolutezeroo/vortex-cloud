using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record SeedNavigatorDefaultsRequest(string Reason) : IReasonedRequest;
