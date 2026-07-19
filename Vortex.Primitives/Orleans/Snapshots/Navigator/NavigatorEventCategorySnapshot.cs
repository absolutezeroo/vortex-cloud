using Orleans;

namespace Vortex.Primitives.Orleans.Snapshots.Navigator;

[GenerateSerializer, Immutable]
public record NavigatorEventCategorySnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required string Name { get; init; }
}
