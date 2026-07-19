using Orleans;

namespace Vortex.Primitives.Players.Wallet;

[GenerateSerializer, Immutable]
public sealed record WalletDebitRequest : WalletDebit;
