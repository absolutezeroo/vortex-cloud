using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record SeedNavigatorDefaultsRequest(string Reason) : IReasonedRequest;
