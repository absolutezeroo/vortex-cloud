namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for the content operations, each carrying a mandatory audited <c>Reason</c>.
/// Grouped in one file because they share a capability and a page-per-domain shape.
/// </summary>
public sealed record AchievementRequest(
    int AchievementId,
    string Name,
    string Category,
    int DisplayMethod,
    string Reason
);

public sealed record DeleteAchievementRequest(int AchievementId, string Reason);

public sealed record AchievementLevelRequest(
    int AchievementId,
    int Level,
    string BadgeCode,
    int ProgressRequirement,
    int RewardAmount,
    int RewardType,
    int ScorePoints,
    string Reason
);

public sealed record DeleteAchievementLevelRequest(int LevelId, string Reason);

public sealed record HandItemRequest(
    int HandItemId,
    string Name,
    int Nutrition,
    int Thirst,
    string Reason
);

public sealed record DeleteHandItemRequest(int Id, string Reason);

public sealed record BotRequest(int BotId, string Name, string Motto, string Figure, string Reason);

public sealed record DeleteBotRequest(int BotId, string Reason);

public sealed record CollectionRequest(
    int CollectionId,
    string CollectionCode,
    string Name,
    int BoostScore,
    int Status,
    string? RewardProductCode,
    string? BonusProductCode,
    string Reason
);

public sealed record DeleteCollectionRequest(int CollectionId, string Reason);

public sealed record CollectionItemRequest(
    int ItemId,
    int CollectionId,
    string ProductCode,
    string ItemTypeId,
    int ProductTypeId,
    int Score,
    string Rarity,
    int SortOrder,
    string Reason
);

public sealed record DeleteCollectionItemRequest(int ItemId, string Reason);

public sealed record CurrencyRequest(
    int CurrencyId,
    string Name,
    int CurrencyType,
    int? ActivityPointType,
    bool Enabled,
    int StartingAmount,
    string Reason
);

public sealed record BuildersClubTierRequest(int Level, int FurniLimit, string Reason);

public sealed record DeleteBuildersClubTierRequest(int TierId, string Reason);

public sealed record RentableTermsRequest(
    int FurnitureId,
    int Price,
    int CurrencyTypeId,
    int RentDurationSeconds,
    bool RequiresHc,
    string Reason
);

public sealed record DeleteRentableTermsRequest(int TermsId, string Reason);

public sealed record BadgeGrantRequest(int PlayerId, string BadgeCode, string Reason);

public sealed record EffectGrantRequest(
    int PlayerId,
    int EffectId,
    int DurationSeconds,
    string Reason
);
