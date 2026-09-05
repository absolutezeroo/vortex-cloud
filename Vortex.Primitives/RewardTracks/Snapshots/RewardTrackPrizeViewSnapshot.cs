using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>A prize resolved for one player.</summary>
[GenerateSerializer, Immutable]
public sealed record RewardTrackPrizeViewSnapshot
{
    [Id(0)]
    public required string PrizeId { get; init; }

    [Id(1)]
    public required int RequiredPoints { get; init; }

    /// <summary>The client's <c>productItemTypeId</c>. Written as a short.</summary>
    [Id(2)]
    public required RewardKind Kind { get; init; }

    [Id(3)]
    public required string RewardTypeId { get; init; }

    [Id(4)]
    public required string ExtraParams { get; init; }

    [Id(5)]
    public required int RewardAmount { get; init; }

    [Id(6)]
    public required bool Premium { get; init; }

    /// <summary>Enough points, and premium if the prize needs it. Not "unclaimed".</summary>
    [Id(7)]
    public required bool Available { get; init; }

    [Id(8)]
    public required bool Claimed { get; init; }

    /// <summary>Available and not yet taken — the only state a claim can move from.</summary>
    public bool Claimable => Available && !Claimed;
}
