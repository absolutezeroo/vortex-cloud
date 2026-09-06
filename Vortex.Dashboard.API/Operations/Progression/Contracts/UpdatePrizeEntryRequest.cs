using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record UpdatePrizeEntryRequest(
    int EntryId,
    string PoolCode,
    string Variant,
    string ProductType,
    int FurnitureDefinitionId,
    string ExtraParam,
    int Weight,
    bool Enabled,
    string Reason
) : IReasonedRequest;
