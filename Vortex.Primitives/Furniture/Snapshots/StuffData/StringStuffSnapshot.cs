using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Furniture.Snapshots.StuffData;

[GenerateSerializer, Immutable]
public sealed record StringStuffSnapshot : StuffDataSnapshot
{
    [Id(0)]
    public required ImmutableArray<string> Data { get; init; }
}
