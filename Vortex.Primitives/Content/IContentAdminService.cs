using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Content;

/// <summary>
/// Writes for the content the dashboard's read surfaces describe: achievement ladders, bots and hand
/// items, NFT collections, and the economy's smaller tables.
/// <para>
/// One service rather than one per domain because the split would be arbitrary — each of these is a
/// handful of rows edited from one page, and every one of them lives behind the same
/// <c>dashboard.ops.content.manage</c> capability. What is <em>not</em> uniform is the live-cache
/// obligation, and each implementation documents its own: achievements and collections reload their
/// manager grain, currencies reload the reference-data provider, hand items are read per use and
/// need nothing.
/// </para>
/// </summary>
public interface IContentAdminService
{
    Task<ContentAdminResult> CreateAchievementAsync(AchievementSpec spec, CancellationToken ct);

    Task<ContentAdminResult> UpdateAchievementAsync(
        int achievementId,
        AchievementSpec spec,
        CancellationToken ct
    );

    Task<ContentAdminResult> DeleteAchievementAsync(int achievementId, CancellationToken ct);

    /// <summary>Creates the level or overwrites the existing one with the same number.</summary>
    Task<ContentAdminResult> UpsertAchievementLevelAsync(
        int achievementId,
        AchievementLevelSpec spec,
        CancellationToken ct
    );

    Task<ContentAdminResult> DeleteAchievementLevelAsync(int levelId, CancellationToken ct);

    Task<ContentAdminResult> UpsertHandItemAsync(HandItemSpec spec, CancellationToken ct);

    Task<ContentAdminResult> DeleteHandItemAsync(int handItemId, CancellationToken ct);

    Task<ContentAdminResult> UpdateBotAsync(int botId, BotSpec spec, CancellationToken ct);

    Task<ContentAdminResult> DeleteBotAsync(int botId, CancellationToken ct);

    Task<ContentAdminResult> CreateCollectionAsync(NftCollectionSpec spec, CancellationToken ct);

    Task<ContentAdminResult> UpdateCollectionAsync(
        int collectionId,
        NftCollectionSpec spec,
        CancellationToken ct
    );

    Task<ContentAdminResult> DeleteCollectionAsync(int collectionId, CancellationToken ct);

    Task<ContentAdminResult> CreateCollectionItemAsync(
        int collectionId,
        NftCollectionItemSpec spec,
        CancellationToken ct
    );

    Task<ContentAdminResult> UpdateCollectionItemAsync(
        int itemId,
        NftCollectionItemSpec spec,
        CancellationToken ct
    );

    Task<ContentAdminResult> DeleteCollectionItemAsync(int itemId, CancellationToken ct);

    Task<ContentAdminResult> CreateCurrencyAsync(CurrencySpec spec, CancellationToken ct);

    Task<ContentAdminResult> UpdateCurrencyAsync(
        int currencyId,
        CurrencySpec spec,
        CancellationToken ct
    );

    Task<ContentAdminResult> UpsertBuildersClubTierAsync(
        BuildersClubTierSpec spec,
        CancellationToken ct
    );

    Task<ContentAdminResult> DeleteBuildersClubTierAsync(int tierId, CancellationToken ct);

    Task<ContentAdminResult> UpsertRentableSpaceTermsAsync(
        RentableSpaceTermsSpec spec,
        CancellationToken ct
    );

    Task<ContentAdminResult> DeleteRentableSpaceTermsAsync(int termsId, CancellationToken ct);

    /// <summary>Grants a badge to a player, live: the badge grain is told, not just the table.</summary>
    Task<ContentAdminResult> GrantBadgeAsync(int playerId, string badgeCode, CancellationToken ct);

    Task<ContentAdminResult> RevokeBadgeAsync(int playerId, string badgeCode, CancellationToken ct);

    Task<ContentAdminResult> GrantEffectAsync(
        int playerId,
        int effectId,
        int durationSeconds,
        CancellationToken ct
    );

    Task<ContentAdminResult> RevokeEffectAsync(int playerId, int effectId, CancellationToken ct);
}
