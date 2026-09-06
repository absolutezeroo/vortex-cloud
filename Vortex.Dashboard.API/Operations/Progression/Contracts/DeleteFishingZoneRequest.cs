using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteFishingZoneRequest(int ZoneId, string Reason) : IReasonedRequest;
