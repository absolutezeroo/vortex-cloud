using Orleans;

namespace Vortex.Primitives.Groups.Snapshots;

/// <summary>One element of a guild badge: a part id, its color and its placement slot.</summary>
[GenerateSerializer, Immutable]
public sealed record GroupBadgePartSnapshot
{
    [Id(0)]
    public required int PartId { get; init; }

    [Id(1)]
    public required int ColorId { get; init; }

    [Id(2)]
    public required int Position { get; init; }
}
