using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Players.Snapshots;

/// <summary>
/// A player's full achievements view: every achievement's standing, the tab the client should open
/// by default, and the player's total achievement score.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record AchievementListSnapshot
{
    [Id(0)]
    public required ImmutableArray<AchievementProgressSnapshot> Achievements { get; init; }

    [Id(1)]
    public required string DefaultCategory { get; init; }

    [Id(2)]
    public required int Score { get; init; }
}
