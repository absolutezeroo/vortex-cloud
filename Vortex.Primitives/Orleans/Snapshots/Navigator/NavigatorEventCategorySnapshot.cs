using Orleans;

namespace Vortex.Primitives.Orleans.Snapshots.Navigator;

[GenerateSerializer, Immutable]
public record NavigatorEventCategorySnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required string Name { get; init; }

    /// <summary>Third field of every entry in UserEventCatsComposer (1370). The client keeps both
    /// an all- and a visible-list and filters on this flag itself, so it must be on the wire.</summary>
    [Id(2)]
    public required bool Visible { get; init; }
}
