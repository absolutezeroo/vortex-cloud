using System;
using Orleans;
using Turbo.Primitives.Players.Enums.Wallet;

namespace Turbo.Primitives.Catalog.Snapshots;

[GenerateSerializer, Immutable]
public sealed record VoucherSnapshot
{
    [Id(0)]
    public required bool Exists { get; init; }

    [Id(1)]
    public required string Code { get; init; }

    [Id(2)]
    public required bool IsActive { get; init; }

    [Id(3)]
    public required CurrencyType CurrencyType { get; init; }

    [Id(4)]
    public int? ActivityPointType { get; init; }

    [Id(5)]
    public required int Amount { get; init; }

    [Id(6)]
    public int? MaxRedemptions { get; init; }

    [Id(7)]
    public required int RedemptionCount { get; init; }

    [Id(8)]
    public DateTime? ExpiresAt { get; init; }
}
