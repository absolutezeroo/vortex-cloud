namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One thing a milestone hands over.
/// </summary>
/// <param name="RewardTypeId">What the kind above points at -- a badge code, a definition id, a
/// currency type. <see cref="RewardKindOption.Target"/> says which, per kind.</param>
public sealed record RewardTrackRewardRow(
    int Id,
    string Kind,
    int KindValue,
    string RewardTypeId,
    int Amount,
    string ExtraParams,
    int SortOrder
);
