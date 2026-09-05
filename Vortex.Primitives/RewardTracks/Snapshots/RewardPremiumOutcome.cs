using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>The outcome of a premium purchase.</summary>
[GenerateSerializer, Immutable]
public readonly record struct RewardPremiumOutcome
{
    [Id(0)]
    public required RewardPremiumResult Result { get; init; }

    [Id(1)]
    public required string TrackId { get; init; }

    /// <summary>The player's points after the purchase, instant points included.</summary>
    [Id(2)]
    public required int Points { get; init; }

    public static RewardPremiumOutcome Fail(RewardPremiumResult result, string trackId) =>
        new()
        {
            Result = result,
            TrackId = trackId,
            Points = 0,
        };
}
