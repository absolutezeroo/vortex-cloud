using System;

namespace Vortex.Primitives.Quests.Admin;

/// <summary>
/// Outcome of a quest admin write. Success/error-code shape mirroring the catalog admin result, but
/// the quest admin service is a plain in-process singleton (not a grain), so no Orleans attributes.
/// </summary>
public sealed record QuestAdminResult(bool Success, int? Id, string? ErrorCode)
{
    public static QuestAdminResult Ok(int id) => new(true, id, null);

    public static QuestAdminResult Fail(string errorCode) => new(false, null, errorCode);
}

/// <summary>
/// Create/update spec for a quest definition. <paramref name="RewardType"/> follows the grant rule
/// in <c>PlayerQuestGrain.GrantRewardAsync</c>: a negative value grants Credits, otherwise it is the
/// activity-point currency type granted on completion (0 = Duckets). <paramref name="Seasonal"/> +
/// <paramref name="SeasonalSeconds"/>/<paramref name="EndsAt"/> drive the configurable countdown
/// (e.g. 9 minutes, 14 days).
/// </summary>
public sealed record QuestCreateSpec(
    string CampaignCode,
    string ChainCode,
    string LocalizationCode,
    string QuestType,
    string TargetType,
    string TargetValue,
    bool Enabled,
    int TotalSteps,
    int RewardType,
    int RewardAmount,
    string CatalogPageName,
    string ImageVersion,
    int SortOrder,
    bool Easy,
    bool Seasonal,
    int SeasonalSeconds,
    DateTime? EndsAt
);

public sealed record QuestUpdateSpec(
    string CampaignCode,
    string ChainCode,
    string LocalizationCode,
    string QuestType,
    string TargetType,
    string TargetValue,
    bool Enabled,
    int TotalSteps,
    int RewardType,
    int RewardAmount,
    string CatalogPageName,
    string ImageVersion,
    int SortOrder,
    bool Easy,
    bool Seasonal,
    int SeasonalSeconds,
    DateTime? EndsAt
);
