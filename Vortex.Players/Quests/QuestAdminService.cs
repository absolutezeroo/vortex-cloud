using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Quests;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Quests;
using Vortex.Primitives.Quests.Admin;

namespace Vortex.Players.Quests;

/// <summary>
/// CRUD for the <c>quests</c> table. A plain singleton (not a grain) opening a short-lived
/// <see cref="VortexDbContext"/> per call: quest rows aren't grain-owned and admin writes are
/// low-frequency. The live quests come from the kept-alive <see cref="Grains.QuestManagerGrain"/>
/// cache, which is only rebuilt via its <c>ReloadAsync</c>; every write here reloads it afterwards so
/// the DB and the live cache never drift — the "DB write not reflected in live state" bug class
/// called out in AGENTS.md.
/// </summary>
internal sealed class QuestAdminService(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    IGrainFactory grainFactory,
    ILogger<QuestAdminService> logger
) : IQuestAdminService
{
    public async Task<QuestAdminResult> CreateAsync(QuestCreateSpec spec, CancellationToken ct)
    {
        if (Validate(spec.CampaignCode, spec.LocalizationCode, spec.QuestType) is { } error)
        {
            return QuestAdminResult.Fail(error);
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        QuestEntity entity = new()
        {
            CampaignCode = spec.CampaignCode.Trim(),
            ChainCode = spec.ChainCode ?? string.Empty,
            LocalizationCode = spec.LocalizationCode.Trim(),
            QuestType = spec.QuestType.Trim(),
            TargetType = (spec.TargetType ?? string.Empty).Trim(),
            TargetValue = (spec.TargetValue ?? string.Empty).Trim(),
            Enabled = spec.Enabled,
            TotalSteps = Math.Max(1, spec.TotalSteps),
            RewardType = spec.RewardType,
            RewardAmount = Math.Max(0, spec.RewardAmount),
            CatalogPageName = spec.CatalogPageName ?? string.Empty,
            ImageVersion = spec.ImageVersion ?? string.Empty,
            SortOrder = spec.SortOrder,
            Easy = spec.Easy,
            Seasonal = spec.Seasonal,
            SeasonalSeconds = Math.Max(0, spec.SeasonalSeconds),
            EndsAt = spec.EndsAt,
        };

        db.Quests.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return QuestAdminResult.Ok(entity.Id);
    }

    public async Task<QuestAdminResult> UpdateAsync(
        int questId,
        QuestUpdateSpec spec,
        CancellationToken ct
    )
    {
        if (Validate(spec.CampaignCode, spec.LocalizationCode, spec.QuestType) is { } error)
        {
            return QuestAdminResult.Fail(error);
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        QuestEntity? entity = await db
            .Quests.FirstOrDefaultAsync(q => q.Id == questId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return QuestAdminResult.Fail("quest_not_found");
        }

        entity.CampaignCode = spec.CampaignCode.Trim();
        entity.ChainCode = spec.ChainCode ?? string.Empty;
        entity.LocalizationCode = spec.LocalizationCode.Trim();
        entity.QuestType = spec.QuestType.Trim();
        entity.TargetType = (spec.TargetType ?? string.Empty).Trim();
        entity.TargetValue = (spec.TargetValue ?? string.Empty).Trim();
        entity.Enabled = spec.Enabled;
        entity.TotalSteps = Math.Max(1, spec.TotalSteps);
        entity.RewardType = spec.RewardType;
        entity.RewardAmount = Math.Max(0, spec.RewardAmount);
        entity.CatalogPageName = spec.CatalogPageName ?? string.Empty;
        entity.ImageVersion = spec.ImageVersion ?? string.Empty;
        entity.SortOrder = spec.SortOrder;
        entity.Easy = spec.Easy;
        entity.Seasonal = spec.Seasonal;
        entity.SeasonalSeconds = Math.Max(0, spec.SeasonalSeconds);
        entity.EndsAt = spec.EndsAt;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return QuestAdminResult.Ok(entity.Id);
    }

    public async Task<QuestAdminResult> DeleteAsync(int questId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        QuestEntity? entity = await db
            .Quests.FirstOrDefaultAsync(q => q.Id == questId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return QuestAdminResult.Fail("quest_not_found");
        }

        // Per-player quest rows reference the quest; a hard delete would either orphan them or hit the
        // FK. Block it once anyone has progress and steer the admin to editing/deactivating instead,
        // mirroring the targeted-offer admin's "has purchases" guard.
        bool hasProgress = await db
            .PlayerQuests.AnyAsync(p => p.QuestEntityId == questId, ct)
            .ConfigureAwait(false);

        if (hasProgress)
        {
            return QuestAdminResult.Fail("quest_has_progress");
        }

        db.Quests.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return QuestAdminResult.Ok(questId);
    }

    private static string? Validate(
        string? campaignCode,
        string? localizationCode,
        string? questType
    )
    {
        if (string.IsNullOrWhiteSpace(campaignCode))
        {
            return "campaign_code_required";
        }

        if (string.IsNullOrWhiteSpace(localizationCode))
        {
            return "localization_code_required";
        }

        if (string.IsNullOrWhiteSpace(questType))
        {
            return "quest_type_required";
        }

        return null;
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        try
        {
            await grainFactory.GetQuestManagerGrain().ReloadAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The DB write already committed -- the live quest cache is now stale until the next
            // reload or restart. Never swallow this: it is the "DB write not reflected in live state"
            // bug class called out in AGENTS.md.
            logger.LogError(
                ex,
                "Quest cache reload failed after an admin write committed -- live quests are now stale until the next reload or restart"
            );
            throw;
        }
    }
}
