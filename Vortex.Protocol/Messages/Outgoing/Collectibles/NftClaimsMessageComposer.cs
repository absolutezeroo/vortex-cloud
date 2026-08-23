using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>
/// The claims waiting on a wallet. An empty list is the honest answer for a hotel with no chain,
/// and it is an answer — the tab asks and waits.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record NftClaimsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<NftClaimSnapshot> Claims { get; init; }
}
