using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One prize this player claimed.
/// </summary>
/// <param name="Granted">What was actually handed over, rendered at claim time. The prize
/// definition can be rewritten afterwards; this cannot, which is what makes "why does this player
/// have that?" answerable a year later.</param>
public sealed record PlayerRewardTrackClaimRow(
    string PrizeId,
    DateTime ClaimedAt,
    int PointsAtClaim,
    string Granted
);
