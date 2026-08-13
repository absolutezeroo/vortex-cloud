using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

/// <summary>Mint-token bundles for sale. Empty here: nothing to spend them on.</summary>
[GenerateSerializer, Immutable]
public sealed record CollectibleMintTokenOffersMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<MintTokenOfferSnapshot> Offers { get; init; }
}
