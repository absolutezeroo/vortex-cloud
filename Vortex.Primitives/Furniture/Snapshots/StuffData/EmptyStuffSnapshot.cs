using Orleans;

namespace Vortex.Primitives.Furniture.Snapshots.StuffData;

[GenerateSerializer, Immutable]
public sealed record EmptyStuffSnapshot : StuffDataSnapshot { }
