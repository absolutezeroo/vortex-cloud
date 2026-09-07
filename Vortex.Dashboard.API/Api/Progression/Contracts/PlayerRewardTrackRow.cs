using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// Where one player stands on one track.
/// </summary>
/// <param name="ContentVersion">The version they are playing. A track edited since is a track whose
/// prizes may no longer be the ones this player was working towards.</param>
public sealed record PlayerRewardTrackRow(
    string TrackId,
    int Points,
    bool PremiumUnlocked,
    DateTime? PremiumUnlockedAt,
    DateTime? CompletedAt,
    int ContentVersion,
    IReadOnlyList<PlayerRewardTrackTaskRow> Tasks,
    IReadOnlyList<PlayerRewardTrackClaimRow> Claims
);
