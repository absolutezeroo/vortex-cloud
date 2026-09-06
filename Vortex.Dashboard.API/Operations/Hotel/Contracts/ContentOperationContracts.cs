using System;
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

public sealed record MintableItemTypeRequest(
    int TypeId,
    string ProductCode,
    int StampPrice,
    DateTime StartsAt,
    DateTime EndsAt,
    bool RegionLocked,
    bool LimitedEdition,
    int EditionSize,
    bool Enabled,
    int SortOrder,
    string Reason
) : IReasonedRequest;

public sealed record DeleteMintableItemTypeRequest(int TypeId, string Reason) : IReasonedRequest;

public sealed record MintTokenOfferRequest(
    int OfferId,
    string ProductCode,
    int SilverPrice,
    int AmountTokens,
    bool Enabled,
    int SortOrder,
    string Reason
) : IReasonedRequest;

public sealed record DeleteMintTokenOfferRequest(int OfferId, string Reason) : IReasonedRequest;

public sealed record ClaimRequest(
    int PlayerId,
    string ProductCode,
    string SetId,
    string DefaultCollectionName,
    string Collection,
    int ClaimLimit,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    string Reason
) : IReasonedRequest;

public sealed record DeleteClaimRequest(int ClaimId, string Reason) : IReasonedRequest;

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

public sealed record NftAvatarRequest(
    string AvatarCode,
    string Name,
    string Figure,
    string Gender,
    string ContractKey,
    int EditionSize,
    bool Enabled,
    int SortOrder,
    string Reason
) : IReasonedRequest;

public sealed record UpdateNftAvatarRequest(
    int AvatarId,
    string AvatarCode,
    string Name,
    string Figure,
    string Gender,
    string ContractKey,
    int EditionSize,
    bool Enabled,
    int SortOrder,
    string Reason
) : IReasonedRequest;

public sealed record DeleteNftAvatarRequest(int AvatarId, string Reason) : IReasonedRequest;

/// <summary>
/// <see cref="Note"/> is the provenance: what the copy was given for. It is carried separately from
/// the audit's own reason because it is the line read back months later, from the avatar's page
/// rather than from the log.
/// </summary>
public sealed record NftAvatarGrantRequest(int AvatarId, int PlayerId, string Note, string Reason)
    : IReasonedRequest;

public sealed record NftAvatarRevokeRequest(int CopyId, string Reason) : IReasonedRequest;
