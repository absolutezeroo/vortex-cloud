using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

/// <summary>
/// The wallets a player has linked. The stardust one is written first and on its own, because the
/// client treats an empty string there as "none linked" rather than as an empty address.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record CollectibleWalletAddressesMessageComposer : IComposer
{
    [Id(0)]
    public string StardustWalletAddress { get; init; } = string.Empty;

    [Id(1)]
    public ImmutableArray<string> WalletAddresses { get; init; } = [];
}
