using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>Every collection the hotel runs, as the viewing player stands in them.</summary>
[GenerateSerializer, Immutable]
public sealed record NftCollectionsMessageComposer : IComposer
{
    [Id(0)]
    public ImmutableArray<NftCollectionSnapshot> Collections { get; init; } = [];
}
