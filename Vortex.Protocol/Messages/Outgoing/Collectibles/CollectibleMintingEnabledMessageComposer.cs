using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>
/// Whether this hotel mints collectibles. Minting is a blockchain errand — it needs a chain, a
/// wallet and a token contract — so an emulator answers no, and the client hides the whole minting
/// half of the interface rather than offering buttons that cannot work.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record CollectibleMintingEnabledMessageComposer : IComposer
{
    [Id(0)]
    public required bool Enabled { get; init; }
}
