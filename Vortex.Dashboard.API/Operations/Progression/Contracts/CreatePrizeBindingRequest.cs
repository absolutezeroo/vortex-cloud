using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record CreatePrizeBindingRequest(
    int FurnitureDefinitionId,
    string PoolCode,
    int HitsRequired,
    bool Enabled,
    string Reason
) : IReasonedRequest;
