using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record UpdateFishingZoneRequest(
    int ZoneId,
    string NameKey,
    string FurniClass,
    int RequiredLevel,
    int MinCatches,
    int MaxCatches,
    string Reason
) : IReasonedRequest;
