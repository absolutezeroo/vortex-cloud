using Orleans;

namespace Vortex.Primitives.Collectibles;

/// <summary>
/// One collectible asset a player holds, as the inventory's Collectibles tab lists it.
/// </summary>
/// <remarks>
/// The asset id comes <em>first</em>, ahead of the product struct rather than after it: the client's
/// class reads its own field before calling into its base constructor. And the struct it inherits is
/// the base one, which reads no amount — an asset is a single item, so there is nothing to count.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record CollectibleAssetSnapshot
{
    /// <summary>A long on the wire. It keys the tab's grid, so it has to be stable per asset.</summary>
    [Id(0)]
    public required long AssetId { get; init; }

    [Id(1)]
    public required CollectibleProductItemSnapshot Product { get; init; }
}
