using Orleans;

namespace Vortex.Primitives.Furniture.Snapshots.StuffData;

[GenerateSerializer, Immutable]
public sealed record LegacyStuffSnapshot : StuffDataSnapshot
{
    [Id(0)]
    public required string Data { get; init; }
}
