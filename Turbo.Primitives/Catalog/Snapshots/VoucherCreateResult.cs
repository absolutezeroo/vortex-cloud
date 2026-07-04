using Orleans;

namespace Turbo.Primitives.Catalog.Snapshots;

[GenerateSerializer, Immutable]
public sealed record VoucherCreateResult
{
    [Id(0)]
    public required bool Success { get; init; }

    [Id(1)]
    public required string ErrorCode { get; init; }
}
