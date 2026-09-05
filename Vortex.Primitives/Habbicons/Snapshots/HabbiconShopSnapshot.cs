using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Habbicons.Snapshots;

/// <summary>Everything the client needs to draw the Habbicon hub for one player.</summary>
[GenerateSerializer, Immutable]
public sealed record HabbiconShopSnapshot
{
    [Id(0)]
    public required ImmutableArray<HabbiconShopCollectionSnapshot> Collections { get; init; }
}
