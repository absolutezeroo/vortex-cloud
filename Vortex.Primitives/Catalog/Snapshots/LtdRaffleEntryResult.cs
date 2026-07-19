using Orleans;

namespace Vortex.Primitives.Catalog.Snapshots;

[GenerateSerializer, Immutable]
public sealed record LtdRaffleEntryResult
{
    [Id(0)]
    public required bool Success { get; init; }

    [Id(1)]
    public required string ErrorCode { get; init; }

    [Id(2)]
    public int? SerialNumber { get; init; }
}
