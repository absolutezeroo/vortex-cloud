using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Players.Snapshots;

/// <summary>
/// A cached achievement definition: its header plus its ordered levels. <see cref="Name"/> is the
/// identifier used to build badge codes (<c>"ACH_" + Name + level</c>); <see cref="Levels"/> is
/// ordered by level ascending.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record AchievementDefinitionSnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required string Name { get; init; }

    [Id(2)]
    public required string Category { get; init; }

    [Id(3)]
    public required int DisplayMethod { get; init; }

    [Id(4)]
    public required ImmutableArray<AchievementLevelSnapshot> Levels { get; init; }
}
