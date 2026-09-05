using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>Premium configuration for a track. Absent means the track has no premium tier.</summary>
[GenerateSerializer, Immutable]
public sealed record RewardTrackPremiumSnapshot
{
    /// <summary>
    /// The task-points multiplier, in per-mille. 1200 = 1.2× = the client's "20% faster
    /// progression". Integer so the grant is exact and reproducible; the wire wants a double, and
    /// that conversion happens once, in the serializer.
    /// </summary>
    [Id(0)]
    public required int BoostPerMille { get; init; }

    /// <summary>Points credited the moment premium is bought.</summary>
    [Id(1)]
    public required int InstantPoints { get; init; }

    [Id(2)]
    public required int CostCredits { get; init; }

    [Id(3)]
    public required int CostDiamonds { get; init; }

    public double BoostMultiplier => BoostPerMille / 1000d;
}
