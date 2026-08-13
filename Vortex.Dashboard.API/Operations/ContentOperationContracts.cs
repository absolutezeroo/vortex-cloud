using Vortex.Dashboard.API.Hosting;

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
) : IReasonedRequest;

public sealed record DeleteAchievementRequest(int AchievementId, string Reason) : IReasonedRequest;

public sealed record AchievementLevelRequest(
    int AchievementId,
    int Level,
    string BadgeCode,
    int ProgressRequirement,
    int RewardAmount,
    int RewardType,
    int ScorePoints,
    string Reason
) : IReasonedRequest;

public sealed record DeleteAchievementLevelRequest(int LevelId, string Reason) : IReasonedRequest;

public sealed record HandItemRequest(
    int HandItemId,
    string Name,
    int Nutrition,
    int Thirst,
    string Reason
) : IReasonedRequest;

public sealed record DeleteHandItemRequest(int Id, string Reason) : IReasonedRequest;

public sealed record BotRequest(int BotId, string Name, string Motto, string Figure, string Reason)
    : IReasonedRequest;

public sealed record DeleteBotRequest(int BotId, string Reason) : IReasonedRequest;

public sealed record CollectionRequest(
    int CollectionId,
    string CollectionCode,
    string Name,
    int BoostScore,
    int Status,
    string? RewardProductCode,
    string? BonusProductCode,
    string Reason
) : IReasonedRequest;

public sealed record DeleteCollectionRequest(int CollectionId, string Reason) : IReasonedRequest;

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
) : IReasonedRequest;

public sealed record DeleteCollectionItemRequest(int ItemId, string Reason) : IReasonedRequest;

public sealed record StoreOfferRequest(
    int OfferId,
    string ProductCode,
    int EmeraldPrice,
    bool IsFeatured,
    bool IsLimited,
    int MintLimit,
    string ItemTypeId,
    int ProductTypeId,
    int Score,
    string Rarity,
    bool Enabled,
    int SortOrder,
    string Reason
) : IReasonedRequest;

public sealed record DeleteStoreOfferRequest(int OfferId, string Reason) : IReasonedRequest;

public sealed record CurrencyRequest(
    int CurrencyId,
    string Name,
    int CurrencyType,
    int? ActivityPointType,
    bool Enabled,
    int StartingAmount,
    string Reason
) : IReasonedRequest;

public sealed record BuildersClubTierRequest(int Level, int FurniLimit, string Reason)
    : IReasonedRequest;

public sealed record DeleteBuildersClubTierRequest(int TierId, string Reason) : IReasonedRequest;

public sealed record RentableTermsRequest(
    int FurnitureId,
    int Price,
    int CurrencyTypeId,
    int RentDurationSeconds,
    bool RequiresHc,
    string Reason
) : IReasonedRequest;

public sealed record DeleteRentableTermsRequest(int TermsId, string Reason) : IReasonedRequest;

public sealed record BadgeGrantRequest(int PlayerId, string BadgeCode, string Reason)
    : IReasonedRequest;

public sealed record EffectGrantRequest(
    int PlayerId,
    int EffectId,
    int DurationSeconds,
    string Reason
) : IReasonedRequest;
