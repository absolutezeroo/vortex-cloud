using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One player's standing on every track they have touched.</summary>
public sealed record PlayerRewardTracks(
    int PlayerId,
    int Count,
    IReadOnlyList<PlayerRewardTrackRow> Items
);
