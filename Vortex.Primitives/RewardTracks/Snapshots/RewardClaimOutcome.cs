using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>The outcome of one prize claim.</summary>
[GenerateSerializer, Immutable]
public readonly record struct RewardClaimOutcome
{
    [Id(0)]
    public required RewardClaimResult Result { get; init; }

    [Id(1)]
    public required string TrackId { get; init; }

    [Id(2)]
    public required string PrizeId { get; init; }

    public bool Succeeded => Result == RewardClaimResult.Success;

    public static RewardClaimOutcome Fail(
        RewardClaimResult result,
        string trackId,
        string prizeId
    ) =>
        new()
        {
            Result = result,
            TrackId = trackId,
            PrizeId = prizeId,
        };
}
