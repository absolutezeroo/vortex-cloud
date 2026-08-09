using Orleans;

namespace Vortex.Primitives.Collectibles;

/// <summary>Where a player stands as a collector, as the client's own header shows it.</summary>
[GenerateSerializer, Immutable]
public sealed record CollectorScoreSnapshot
{
    [Id(0)]
    public required int Score { get; init; }

    [Id(1)]
    public required int HighestScore { get; init; }

    /// <summary>
    /// How many collections they have finished. Habbo derives a level from a scale nobody outside
    /// it has, so this counts something a player can see for themselves rather than inventing one.
    /// </summary>
    [Id(2)]
    public required int Level { get; init; }
}
