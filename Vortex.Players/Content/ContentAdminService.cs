using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Achievements;
using Vortex.Database.Entities.Collectibles;
using Vortex.Primitives.Content;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Providers;

namespace Vortex.Players.Content;

/// <summary>
/// Writes for the content the dashboard reads: achievement ladders and NFT collections here, the
/// hotel's smaller tables in the sibling partial.
/// <para>
/// Both domains in this file are cached by a kept-alive manager grain that loads once and never
/// re-reads, so every accepted write ends with a reload. A failed reload is rethrown, never
/// swallowed: the row is already committed, and a silent stale cache is the "DB write not reflected
/// in live state" bug class called out in AGENTS.md.
/// </para>
/// </summary>
internal sealed partial class ContentAdminService(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    IGrainFactory grainFactory,
    ICurrencyTypeProvider currencyTypes,
    ILogger<ContentAdminService> logger
) : IContentAdminService
{
    public async Task<ContentAdminResult> CreateAchievementAsync(
        AchievementSpec spec,
        CancellationToken ct
    )
    {
        if (ValidateAchievement(spec) is { } error)
        {
            return ContentAdminResult.Fail(error);
        }

        string name = spec.Name.Trim();

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (await db.Achievements.AnyAsync(a => a.Name == name, ct).ConfigureAwait(false))
        {
            // The name builds every badge code and is what triggers look up, so two rows sharing one
            // would hand the same badge to two ladders.
            return ContentAdminResult.Fail("achievement_name_taken");
        }

        AchievementEntity entity = new()
        {
            Name = name,
            Category = spec.Category.Trim(),
            DisplayMethod = spec.DisplayMethod,
        };

        db.Achievements.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAchievementsAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> UpdateAchievementAsync(
        int achievementId,
        AchievementSpec spec,
        CancellationToken ct
    )
    {
        if (ValidateAchievement(spec) is { } error)
        {
            return ContentAdminResult.Fail(error);
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        AchievementEntity? entity = await db
            .Achievements.FirstOrDefaultAsync(a => a.Id == achievementId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("achievement_not_found");
        }

        string name = spec.Name.Trim();

        if (
            !string.Equals(entity.Name, name, StringComparison.Ordinal)
            && await db
                .Achievements.AnyAsync(a => a.Name == name && a.Id != achievementId, ct)
                .ConfigureAwait(false)
        )
        {
            return ContentAdminResult.Fail("achievement_name_taken");
        }

        entity.Name = name;
        entity.Category = spec.Category.Trim();
        entity.DisplayMethod = spec.DisplayMethod;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAchievementsAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> DeleteAchievementAsync(
        int achievementId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        AchievementEntity? entity = await db
            .Achievements.FirstOrDefaultAsync(a => a.Id == achievementId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("achievement_not_found");
        }

        bool hasProgress = await db
            .PlayerAchievements.AnyAsync(p => p.AchievementEntityId == achievementId, ct)
            .ConfigureAwait(false);

        if (hasProgress)
        {
            // Progress rows carry the FK, and the badges already handed out reference the ladder's
            // codes. Deleting under them would orphan both.
            return ContentAdminResult.Fail("achievement_has_progress");
        }

        List<AchievementLevelEntity> levels = await db
            .AchievementLevels.Where(l => l.AchievementEntityId == achievementId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        db.AchievementLevels.RemoveRange(levels);
        db.Achievements.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAchievementsAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(achievementId);
    }

    public async Task<ContentAdminResult> UpsertAchievementLevelAsync(
        int achievementId,
        AchievementLevelSpec spec,
        CancellationToken ct
    )
    {
        if (spec.Level <= 0)
        {
            return ContentAdminResult.Fail("level_must_be_positive");
        }

        if (string.IsNullOrWhiteSpace(spec.BadgeCode))
        {
            return ContentAdminResult.Fail("badge_code_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (!await db.Achievements.AnyAsync(a => a.Id == achievementId, ct).ConfigureAwait(false))
        {
            return ContentAdminResult.Fail("achievement_not_found");
        }

        AchievementLevelEntity? entity = await db
            .AchievementLevels.FirstOrDefaultAsync(
                l => l.AchievementEntityId == achievementId && l.Level == spec.Level,
                ct
            )
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = new AchievementLevelEntity
            {
                AchievementEntityId = achievementId,
                Level = spec.Level,
                BadgeCode = spec.BadgeCode.Trim(),
                ProgressRequirement = Math.Max(1, spec.ProgressRequirement),
                RewardAmount = Math.Max(0, spec.RewardAmount),
                RewardType = spec.RewardType,
                ScorePoints = Math.Max(0, spec.ScorePoints),
            };

            db.AchievementLevels.Add(entity);
        }
        else
        {
            entity.BadgeCode = spec.BadgeCode.Trim();
            entity.ProgressRequirement = Math.Max(1, spec.ProgressRequirement);
            entity.RewardAmount = Math.Max(0, spec.RewardAmount);
            entity.RewardType = spec.RewardType;
            entity.ScorePoints = Math.Max(0, spec.ScorePoints);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAchievementsAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> DeleteAchievementLevelAsync(
        int levelId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        AchievementLevelEntity? entity = await db
            .AchievementLevels.FirstOrDefaultAsync(l => l.Id == levelId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("level_not_found");
        }

        bool reached = await db
            .PlayerAchievements.AnyAsync(
                p => p.AchievementEntityId == entity.AchievementEntityId && p.Level >= entity.Level,
                ct
            )
            .ConfigureAwait(false);

        if (reached)
        {
            // Someone already holds this rung's badge; removing the row leaves them holding a badge
            // the ladder no longer explains.
            return ContentAdminResult.Fail("level_already_reached");
        }

        db.AchievementLevels.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAchievementsAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(levelId);
    }

    public async Task<ContentAdminResult> CreateCollectionAsync(
        NftCollectionSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.CollectionCode) || string.IsNullOrWhiteSpace(spec.Name))
        {
            return ContentAdminResult.Fail("code_and_name_required");
        }

        string code = spec.CollectionCode.Trim();

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (
            await db
                .NftCollections.AnyAsync(c => c.CollectionCode == code, ct)
                .ConfigureAwait(false)
        )
        {
            return ContentAdminResult.Fail("collection_code_taken");
        }

        NftCollectionEntity entity = new()
        {
            CollectionCode = code,
            Name = spec.Name.Trim(),
            BoostScore = spec.BoostScore,
            Status = spec.Status,
            RewardProductCode = spec.RewardProductCode,
            BonusProductCode = spec.BonusProductCode,
        };

        db.NftCollections.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadCollectionsAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> UpdateCollectionAsync(
        int collectionId,
        NftCollectionSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.CollectionCode) || string.IsNullOrWhiteSpace(spec.Name))
        {
            return ContentAdminResult.Fail("code_and_name_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        NftCollectionEntity? entity = await db
            .NftCollections.FirstOrDefaultAsync(c => c.Id == collectionId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("collection_not_found");
        }

        entity.CollectionCode = spec.CollectionCode.Trim();
        entity.Name = spec.Name.Trim();
        entity.BoostScore = spec.BoostScore;
        entity.Status = spec.Status;
        entity.RewardProductCode = spec.RewardProductCode;
        entity.BonusProductCode = spec.BonusProductCode;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadCollectionsAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> DeleteCollectionAsync(
        int collectionId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        NftCollectionEntity? entity = await db
            .NftCollections.FirstOrDefaultAsync(c => c.Id == collectionId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("collection_not_found");
        }

        List<NftCollectionItemEntity> items = await db
            .NftCollectionItems.Where(i => i.NftCollectionEntityId == collectionId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        db.NftCollectionItems.RemoveRange(items);
        db.NftCollections.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadCollectionsAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(collectionId);
    }

    /// <summary>
    /// The client resolves a collection item's furni with <c>parseInt(itemTypeId)</c>, which stops
    /// at the first non-digit instead of failing — so a classname like <c>11_dragonlamp_skream</c>
    /// is silently read as furni id 11 and the player is shown a Gothic Torch. The value has to be
    /// the numeric furni id from furnidata (that lamp is 38631883), never the classname.
    ///
    /// Empty is allowed: the entity documents it as "falls back to the product code".
    /// </summary>
    private static bool IsValidItemTypeId(string? itemTypeId)
    {
        if (string.IsNullOrWhiteSpace(itemTypeId))
        {
            return true;
        }

        foreach (char c in itemTypeId.Trim())
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    public async Task<ContentAdminResult> CreateCollectionItemAsync(
        int collectionId,
        NftCollectionItemSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.ProductCode))
        {
            return ContentAdminResult.Fail("product_code_required");
        }

        if (!IsValidItemTypeId(spec.ItemTypeId))
        {
            return ContentAdminResult.Fail("item_type_id_must_be_numeric");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (!await db.NftCollections.AnyAsync(c => c.Id == collectionId, ct).ConfigureAwait(false))
        {
            return ContentAdminResult.Fail("collection_not_found");
        }

        NftCollectionItemEntity entity = new()
        {
            NftCollectionEntityId = collectionId,
            ProductCode = spec.ProductCode.Trim(),
            ItemTypeId = spec.ItemTypeId ?? string.Empty,
            ProductTypeId = spec.ProductTypeId,
            Score = Math.Max(0, spec.Score),
            Rarity = spec.Rarity ?? string.Empty,
            SortOrder = spec.SortOrder,
        };

        db.NftCollectionItems.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadCollectionsAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> UpdateCollectionItemAsync(
        int itemId,
        NftCollectionItemSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.ProductCode))
        {
            return ContentAdminResult.Fail("product_code_required");
        }

        if (!IsValidItemTypeId(spec.ItemTypeId))
        {
            return ContentAdminResult.Fail("item_type_id_must_be_numeric");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        NftCollectionItemEntity? entity = await db
            .NftCollectionItems.FirstOrDefaultAsync(i => i.Id == itemId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("item_not_found");
        }

        entity.ProductCode = spec.ProductCode.Trim();
        entity.ItemTypeId = spec.ItemTypeId ?? string.Empty;
        entity.ProductTypeId = spec.ProductTypeId;
        entity.Score = Math.Max(0, spec.Score);
        entity.Rarity = spec.Rarity ?? string.Empty;
        entity.SortOrder = spec.SortOrder;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadCollectionsAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> DeleteCollectionItemAsync(
        int itemId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        NftCollectionItemEntity? entity = await db
            .NftCollectionItems.FirstOrDefaultAsync(i => i.Id == itemId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("item_not_found");
        }

        db.NftCollectionItems.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadCollectionsAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(itemId);
    }

    public async Task<ContentAdminResult> CreateStoreOfferAsync(
        NftStoreOfferSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.ProductCode))
        {
            return ContentAdminResult.Fail("product_code_required");
        }

        string productCode = spec.ProductCode.Trim();

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (
            await db
                .NftStoreOffers.AnyAsync(offer => offer.ProductCode == productCode, ct)
                .ConfigureAwait(false)
        )
        {
            // The client identifies an offer by its product code when it buys, so two rows sharing
            // one code would make the purchase ambiguous.
            return ContentAdminResult.Fail("product_code_taken");
        }

        NftStoreOfferEntity entity = new() { ProductCode = productCode };

        Apply(entity, spec);
        db.NftStoreOffers.Add(entity);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadStoreAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> UpdateStoreOfferAsync(
        int offerId,
        NftStoreOfferSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.ProductCode))
        {
            return ContentAdminResult.Fail("product_code_required");
        }

        string productCode = spec.ProductCode.Trim();

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        NftStoreOfferEntity? entity = await db
            .NftStoreOffers.FirstOrDefaultAsync(offer => offer.Id == offerId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("offer_not_found");
        }

        if (
            await db
                .NftStoreOffers.AnyAsync(
                    offer => offer.ProductCode == productCode && offer.Id != offerId,
                    ct
                )
                .ConfigureAwait(false)
        )
        {
            return ContentAdminResult.Fail("product_code_taken");
        }

        entity.ProductCode = productCode;
        Apply(entity, spec);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadStoreAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> DeleteStoreOfferAsync(int offerId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        NftStoreOfferEntity? entity = await db
            .NftStoreOffers.FirstOrDefaultAsync(offer => offer.Id == offerId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("offer_not_found");
        }

        db.NftStoreOffers.Remove(entity);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadStoreAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(offerId);
    }

    /// <summary>Everything except the product code, which the two callers set themselves because
    /// only they know whether it is allowed to change.</summary>
    private static void Apply(NftStoreOfferEntity entity, NftStoreOfferSpec spec)
    {
        entity.EmeraldPrice = spec.EmeraldPrice;
        entity.IsFeatured = spec.IsFeatured;
        entity.IsLimited = spec.IsLimited;
        entity.MintLimit = spec.MintLimit;
        entity.ItemTypeId = spec.ItemTypeId?.Trim() ?? string.Empty;
        entity.ProductTypeId = spec.ProductTypeId;
        entity.Score = spec.Score;
        entity.Rarity = spec.Rarity?.Trim() ?? string.Empty;
        entity.Enabled = spec.Enabled;
        entity.SortOrder = spec.SortOrder;
    }

    private async Task ReloadStoreAsync(CancellationToken ct)
    {
        try
        {
            await grainFactory.GetNftStoreGrain().ReloadAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Collectibles shop cache reload failed after an admin write committed -- the live shop is stale until the next reload or restart"
            );
            throw;
        }
    }

    public async Task<ContentAdminResult> CreateClaimAsync(NftClaimSpec spec, CancellationToken ct)
    {
        if (spec.PlayerId <= 0 || string.IsNullOrWhiteSpace(spec.ProductCode))
        {
            return ContentAdminResult.Fail("player_and_product_required");
        }

        if (spec.ClaimLimit <= 0)
        {
            return ContentAdminResult.Fail("claim_limit_must_be_positive");
        }

        string productCode = spec.ProductCode.Trim();

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (
            !await db
                .Players.AnyAsync(player => player.Id == spec.PlayerId, ct)
                .ConfigureAwait(false)
        )
        {
            return ContentAdminResult.Fail("player_not_found");
        }

        if (
            !await db
                .FurnitureDefinitions.AnyAsync(
                    definition => definition.Name == productCode && definition.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(false)
        )
        {
            // Refused here rather than at claim time: a reward naming furniture that does not exist
            // would sit in the player's list forever and be skipped every time they claimed.
            return ContentAdminResult.Fail("furniture_not_found");
        }

        NftClaimEntity entity = new()
        {
            PlayerEntityId = spec.PlayerId,
            // The client only ever claims everything at once, so the code is an identifier rather
            // than something an operator needs to choose.
            ClaimCode = Guid.NewGuid().ToString("n")[..16],
            ProductCode = productCode,
            SetId = spec.SetId?.Trim() ?? string.Empty,
            DefaultCollectionName = spec.DefaultCollectionName?.Trim() ?? string.Empty,
            Collection = spec.Collection?.Trim() ?? string.Empty,
            ClaimLimit = spec.ClaimLimit,
            ValidFrom = spec.ValidFrom,
            ValidTo = spec.ValidTo,
        };

        db.NftClaims.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> DeleteClaimAsync(int claimId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        NftClaimEntity? entity = await db
            .NftClaims.FirstOrDefaultAsync(claim => claim.Id == claimId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("claim_not_found");
        }

        db.NftClaims.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(claimId);
    }

    private static string? ValidateAchievement(AchievementSpec spec) =>
        string.IsNullOrWhiteSpace(spec.Name) ? "name_required"
        : string.IsNullOrWhiteSpace(spec.Category) ? "category_required"
        : null;

    private async Task ReloadAchievementsAsync(CancellationToken ct)
    {
        try
        {
            await grainFactory.GetAchievementManagerGrain().ReloadAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Achievement definition cache reload failed after an admin write committed -- live achievements are stale until the next reload or restart"
            );
            throw;
        }
    }

    private async Task ReloadCollectionsAsync(CancellationToken ct)
    {
        try
        {
            await grainFactory.GetNftCollectionsGrain().ReloadAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "NFT collection cache reload failed after an admin write committed -- live collections are stale until the next reload or restart"
            );
            throw;
        }
    }
}
