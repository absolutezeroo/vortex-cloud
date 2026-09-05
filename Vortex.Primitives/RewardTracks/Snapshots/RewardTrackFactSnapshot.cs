using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>One named fact about what a player just did. Key is from <see cref="RewardTrackFacts"/>.</summary>
[GenerateSerializer, Immutable]
public readonly record struct RewardTrackFactSnapshot(
    [property: Id(0)] string Key,
    [property: Id(1)] string Value
);
