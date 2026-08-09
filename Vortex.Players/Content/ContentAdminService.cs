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
