using System;
using Orleans;
using Vortex.Primitives.Players.Enums.Wallet;

namespace Vortex.Primitives.Catalog.Snapshots;

[GenerateSerializer, Immutable]
public sealed record VoucherCreateSpec
{
    [Id(0)]
    public required CurrencyType CurrencyType { get; init; }

    [Id(1)]
    public int? ActivityPointType { get; init; }

    [Id(2)]
    public required int Amount { get; init; }

    [Id(3)]
    public int? MaxRedemptions { get; init; }

    [Id(4)]
    public DateTime? ExpiresAt { get; init; }

    [Id(5)]
    public required string CreatedBy { get; init; }
}
