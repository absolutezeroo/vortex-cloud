using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record CreatePrizePoolRequest(
    string Code,
    string Name,
    string Variants,
    string Notes,
    bool Enabled,
    string Reason
) : IReasonedRequest;

public sealed record UpdatePrizePoolRequest(
    int PoolId,
    string Code,
    string Name,
    string Variants,
    string Notes,
    bool Enabled,
    string Reason
) : IReasonedRequest;

public sealed record DeletePrizePoolRequest(int PoolId, string Reason) : IReasonedRequest;

public sealed record CreatePrizeEntryRequest(
    string PoolCode,
    string Variant,
    string ProductType,
    int FurnitureDefinitionId,
    string ExtraParam,
    int Weight,
    bool Enabled,
    string Reason
) : IReasonedRequest;

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

public sealed record DeletePrizeEntryRequest(int EntryId, string Reason) : IReasonedRequest;

public sealed record ReloadPrizePoolsRequest(string Reason) : IReasonedRequest;

public sealed record CreatePrizeBindingRequest(
    int FurnitureDefinitionId,
    string PoolCode,
    int HitsRequired,
    bool Enabled,
    string Reason
) : IReasonedRequest;

public sealed record UpdatePrizeBindingRequest(
    int BindingId,
    int FurnitureDefinitionId,
    string PoolCode,
    int HitsRequired,
    bool Enabled,
    string Reason
) : IReasonedRequest;

public sealed record DeletePrizeBindingRequest(int BindingId, string Reason) : IReasonedRequest;
