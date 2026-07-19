using Orleans;
using Vortex.Primitives.Players.Wallet;

namespace Vortex.Primitives.Players.Snapshots;

[GenerateSerializer, Immutable]
public sealed record WalletCurrencyUpdateSnapshot
{
    [Id(0)]
    public required CurrencyKind CurrencyKind { get; init; }

    [Id(1)]
    public required int ChangedBy { get; init; }

    [Id(2)]
    public required int Amount { get; init; }
}
