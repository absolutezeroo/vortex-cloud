using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

/// <summary>What can be minted. An empty list is what a hotel with no chain has to offer.</summary>
[GenerateSerializer, Immutable]
public sealed record CollectableMintableItemTypesMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<MintableItemTypeSnapshot> ItemTypes { get; init; }
}
