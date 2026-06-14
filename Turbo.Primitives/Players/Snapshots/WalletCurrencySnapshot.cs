using Orleans;
using Turbo.Primitives.Players.Wallet;

namespace Turbo.Primitives.Players.Snapshots;

[GenerateSerializer, Immutable]
public sealed record WalletCurrencySnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required CurrencyKind CurrencyKind { get; init; }

    [Id(2)]
    public required int Amount { get; init; }
}
