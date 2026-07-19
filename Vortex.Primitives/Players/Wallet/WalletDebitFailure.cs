using Orleans;

namespace Vortex.Primitives.Players.Wallet;

[GenerateSerializer, Immutable]
public sealed record WalletDebitFailure : WalletDebit;
