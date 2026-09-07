namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One thing a daily task hands over.</summary>
public sealed record DailyTaskRewardRow(
    int Id,
    int ProductItemTypeId,
    string RewardTypeId,
    string ExtraParams,
    int Amount
);
