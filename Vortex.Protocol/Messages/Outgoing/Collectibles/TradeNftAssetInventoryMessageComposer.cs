using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>
/// The player's collectible assets, and the only thing that ends the inventory tab's wait.
/// </summary>
/// <remarks>
/// Not answering is not a neutral choice here. The tab treats "list not initialised" as its loading
/// state, and only this message initialises it — so with no reply it shows a spinner over a hidden
/// grid for as long as the inventory stays open. An empty list moves it to its empty state, which is
/// the truth on a hotel with no chain.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record TradeNftAssetInventoryMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<CollectibleAssetSnapshot> Assets { get; init; }
}
