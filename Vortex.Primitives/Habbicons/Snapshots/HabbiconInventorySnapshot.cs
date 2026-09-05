using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Habbicons.Snapshots;

/// <summary>The player's owned list plus their recently-used ids, as the login push sends it.</summary>
[GenerateSerializer, Immutable]
public sealed record HabbiconInventorySnapshot
{
    [Id(0)]
    public required ImmutableArray<PlayerHabbiconSnapshot> Habbicons { get; init; }

    /// <summary>Most recent first, capped by <c>Vortex:Habbicons:RecentLimit</c>.</summary>
    [Id(1)]
    public required ImmutableArray<int> RecentHabbiconIds { get; init; }
}
