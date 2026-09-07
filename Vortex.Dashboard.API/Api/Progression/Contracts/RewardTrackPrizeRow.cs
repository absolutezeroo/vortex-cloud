using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One milestone on a track.
/// </summary>
/// <param name="Reachable">Whether the points it asks for can actually be earned on the side it
/// sits on. False is a prize no player can ever claim.</param>
public sealed record RewardTrackPrizeRow(
    int Id,
    string PrizeId,
    int RequiredPoints,
    bool Premium,
    int SortOrder,
    bool Reachable,
    IReadOnlyList<RewardTrackRewardRow> Rewards
);
