using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Quests;
using Vortex.Players.Quests;
using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Quests.Grains;
using Vortex.Primitives.Quests.Snapshots;

namespace Vortex.Players.Grains;

/// <summary>
/// Per-player quest grain. Resolves the player's stored progress rows against the cached quest
/// definitions into wire-ready snapshots, and owns quest lifecycle (activate/accept/cancel/reject)
/// and objective progression (with completion rewards).
/// </summary>
internal sealed class PlayerQuestGrain(
    IGrainFactory grainFactory,
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<PlayerQuestGrain> logger
) : Grain, IPlayerQuestGrain
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<PlayerQuestGrain> _logger = logger;

    /// <summary>Campaign whose quests form the rotating daily pool (shown via the daily section, not
    /// the main campaign list).</summary>
    private const string DailyCampaign = "daily";

    private int PlayerId => (int)this.GetPrimaryKeyLong();

    public async Task<QuestListSnapshot> GetQuestsAsync(bool openWindow, CancellationToken ct)
    {
        ImmutableArray<QuestSnapshot> all = await BuildAllAsync(ct).ConfigureAwait(true);

        return new QuestListSnapshot
        {
            Quests = [.. all.Where(q => !q.Seasonal && q.CampaignCode != DailyCampaign)],
            OpenWindow = openWindow,
        };
    }

    public async Task<QuestListSnapshot> GetSeasonalQuestsAsync(CancellationToken ct)
    {
        ImmutableArray<QuestSnapshot> all = await BuildAllAsync(ct).ConfigureAwait(true);

        return new QuestListSnapshot
        {
            Quests = [.. all.Where(q => q.Seasonal)],
            OpenWindow = false,
        };
    }

    public async Task<DailyQuestSnapshot> GetDailyQuestAsync(CancellationToken ct)
    {
        ImmutableArray<QuestSnapshot> all = await BuildAllAsync(ct).ConfigureAwait(true);

        List<QuestSnapshot> dailyPool =
        [
            .. all.Where(q => q.CampaignCode == DailyCampaign && !q.Seasonal).OrderBy(q => q.Id),
        ];

        QuestSnapshot? daily = DailyQuestSelector.Pick(
            dailyPool,
            PlayerId,
            DateOnly.FromDateTime(DateTime.Now)
        );

        return new DailyQuestSnapshot
        {
            Quest = daily,
            EasyQuestCount = dailyPool.Count(q => q.Easy),
            HardQuestCount = dailyPool.Count(q => !q.Easy),
        };
    }

    public async Task<QuestSnapshot?> GetTrackedQuestAsync(CancellationToken ct)
    {
        ImmutableArray<QuestSnapshot> all = await BuildAllAsync(ct).ConfigureAwait(true);

        return all.FirstOrDefault(q => q.Accepted && q.CompletedSteps < q.TotalSteps);
    }

    public Task AcceptAsync(int questId, CancellationToken ct) => ActivateAsync(questId, ct);

    public async Task ActivateAsync(int questId, CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<PlayerQuestEntity> rows = await dbCtx
                .PlayerQuests.Where(p => p.PlayerEntityId == PlayerId)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            // One active quest at a time: deactivate any other in-progress quest.
            foreach (
                PlayerQuestEntity other in rows.Where(r =>
                    r.QuestEntityId != questId && r.Accepted && !r.Completed
                )
            )
            {
                other.Accepted = false;
            }

            PlayerQuestEntity? row = rows.FirstOrDefault(r => r.QuestEntityId == questId);
            if (row is null)
            {
                dbCtx.PlayerQuests.Add(
                    new PlayerQuestEntity
                    {
                        PlayerEntityId = PlayerId,
                        QuestEntityId = questId,
                        Accepted = true,
                        AcceptedAt = DateTime.Now,
                    }
                );
            }
            else if (!row.Completed)
            {
                row.Accepted = true;
                row.AcceptedAt = DateTime.Now;
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to activate quest {QuestId} for player {PlayerId}",
                questId,
                PlayerId
            );
            return;
        }

        QuestSnapshot? snapshot = await BuildOneAsync(questId, ct).ConfigureAwait(true);
        if (snapshot is not null)
        {
            await Presence
                .SendComposerAsync(new QuestMessageComposer { Quest = snapshot })
                .ConfigureAwait(true);
        }
    }

    public Task CancelAsync(CancellationToken ct) => UnacceptAsync(questId: null, ct);

    public Task RejectAsync(int questId, CancellationToken ct) => UnacceptAsync(questId, ct);

    public async Task ProgressAsync(string questType, int amount, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(questType) || amount <= 0)
        {
            return;
        }

        ImmutableArray<QuestDefinitionSnapshot> definitions = await _grainFactory
            .GetQuestManagerGrain()
            .GetDefinitionsAsync(ct)
            .ConfigureAwait(true);

        Dictionary<int, QuestDefinitionSnapshot> defByType = definitions
            .Where(d => string.Equals(d.QuestType, questType, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(d => d.Id);

        if (defByType.Count == 0)
        {
            return;
        }

        // Today's daily quest (of this type) is auto-active: it progresses without being manually
        // accepted, so resolve which one it is for this player and day.
        int? dailyQuestId = ResolveTodaysDailyOfType(definitions, defByType);

        List<(int QuestId, bool Completed)> changed = [];

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<int> ids = [.. defByType.Keys];
            List<PlayerQuestEntity> existing = await dbCtx
                .PlayerQuests.Where(p =>
                    p.PlayerEntityId == PlayerId && ids.Contains(p.QuestEntityId)
                )
                .ToListAsync(ct)
                .ConfigureAwait(true);

            // Quests to advance: accepted campaign quests, plus today's daily (auto-active).
            List<PlayerQuestEntity> toProcess =
            [
                .. existing.Where(r => r.Accepted && !r.Completed),
            ];

            if (dailyQuestId is int did)
            {
                PlayerQuestEntity? dailyRow = existing.FirstOrDefault(r => r.QuestEntityId == did);
                if (dailyRow is null)
                {
                    dailyRow = new PlayerQuestEntity
                    {
                        PlayerEntityId = PlayerId,
                        QuestEntityId = did,
                        Accepted = true,
                        AcceptedAt = DateTime.Now,
                    };
                    dbCtx.PlayerQuests.Add(dailyRow);
                    toProcess.Add(dailyRow);
                }
                else if (!dailyRow.Completed && !toProcess.Contains(dailyRow))
                {
                    toProcess.Add(dailyRow);
                }
            }

            if (toProcess.Count == 0)
            {
                return;
            }

            foreach (PlayerQuestEntity row in toProcess)
            {
                QuestDefinitionSnapshot def = defByType[row.QuestEntityId];
                (int newSteps, bool done) = QuestProgressCalculator.Apply(
                    row.CompletedSteps,
                    amount,
                    def.TotalSteps
                );
                row.CompletedSteps = newSteps;
                row.Completed = done;
                changed.Add((row.QuestEntityId, done));
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to progress quests of type {Type} for player {PlayerId}",
                questType,
                PlayerId
            );
            return;
        }

        ImmutableArray<QuestSnapshot> all = await BuildAllAsync(ct).ConfigureAwait(true);
        Dictionary<int, QuestSnapshot> byId = all.ToDictionary(q => q.Id);

        foreach ((int questId, bool done) in changed)
        {
            if (!byId.TryGetValue(questId, out QuestSnapshot? snapshot))
            {
                continue;
            }

            if (done)
            {
                await GrantRewardAsync(
                        snapshot.ActivityPointType,
                        snapshot.RewardCurrencyAmount,
                        ct
                    )
                    .ConfigureAwait(true);
                await Presence
                    .SendComposerAsync(
                        new QuestCompletedMessageComposer { Quest = snapshot, ShowDialog = true }
                    )
                    .ConfigureAwait(true);
            }
            else
            {
                await Presence
                    .SendComposerAsync(new QuestMessageComposer { Quest = snapshot })
                    .ConfigureAwait(true);
            }
        }
    }

    /// <summary>
    /// Resolves this player's daily quest for today and returns its id only if its type is among
    /// <paramref name="defByType"/> (i.e. the current trigger can advance it); otherwise null.
    /// </summary>
    private int? ResolveTodaysDailyOfType(
        ImmutableArray<QuestDefinitionSnapshot> definitions,
        Dictionary<int, QuestDefinitionSnapshot> defByType
    )
    {
        List<QuestDefinitionSnapshot> dailyDefs =
        [
            .. definitions.Where(d => d.CampaignCode == DailyCampaign).OrderBy(d => d.Id),
        ];

        int index = DailyQuestSelector.PickIndex(
            dailyDefs.Count,
            PlayerId,
            DateOnly.FromDateTime(DateTime.Now)
        );

        if (index < 0)
        {
            return null;
        }

        int questId = dailyDefs[index].Id;
        return defByType.ContainsKey(questId) ? questId : null;
    }

    private async Task UnacceptAsync(int? questId, CancellationToken ct)
    {
        int targetQuestId;

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            PlayerQuestEntity? row = questId is int id
                ? await dbCtx
                    .PlayerQuests.FirstOrDefaultAsync(
                        p => p.PlayerEntityId == PlayerId && p.QuestEntityId == id,
                        ct
                    )
                    .ConfigureAwait(true)
                // No id (client "cancel"): the single active, in-progress quest.
                : await dbCtx
                    .PlayerQuests.Where(p =>
                        p.PlayerEntityId == PlayerId && p.Accepted && !p.Completed
                    )
                    .OrderByDescending(p => p.AcceptedAt)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(true);

            if (row is null || !row.Accepted)
            {
                return;
            }

            row.Accepted = false;
            targetQuestId = row.QuestEntityId;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel quest for player {PlayerId}", PlayerId);
            return;
        }

        QuestSnapshot? snapshot = await BuildOneAsync(targetQuestId, ct).ConfigureAwait(true);
        if (snapshot is not null)
        {
            await Presence
                .SendComposerAsync(
                    new QuestCancelledMessageComposer { Expired = false, Quest = snapshot }
                )
                .ConfigureAwait(true);
        }
    }

    private async Task GrantRewardAsync(int rewardType, int rewardAmount, CancellationToken ct)
    {
        if (rewardAmount <= 0)
        {
            return;
        }

        IPlayerWalletGrain wallet = _grainFactory.GetPlayerWalletGrain((long)PlayerId);

        if (rewardType < 0)
        {
            await wallet.GrantCreditsAsync(rewardAmount, ct).ConfigureAwait(true);
        }
        else
        {
            await wallet
                .GrantActivityPointsAsync(rewardType, rewardAmount, ct)
                .ConfigureAwait(true);
        }
    }

    private async Task<QuestSnapshot?> BuildOneAsync(int questId, CancellationToken ct)
    {
        ImmutableArray<QuestSnapshot> all = await BuildAllAsync(ct).ConfigureAwait(true);
        return all.FirstOrDefault(q => q.Id == questId);
    }

    private IPlayerPresenceGrain Presence => _grainFactory.GetPlayerPresenceGrain((long)PlayerId);

    private async Task<ImmutableArray<QuestSnapshot>> BuildAllAsync(CancellationToken ct)
    {
        ImmutableArray<QuestDefinitionSnapshot> definitions = await _grainFactory
            .GetQuestManagerGrain()
            .GetDefinitionsAsync(ct)
            .ConfigureAwait(true);

        if (definitions.IsEmpty)
        {
            return ImmutableArray<QuestSnapshot>.Empty;
        }

        Dictionary<int, PlayerQuestEntity> progressByQuest = await LoadProgressAsync(ct)
            .ConfigureAwait(true);

        // Campaign aggregates: how many quests each campaign has, and how many the player completed.
        Dictionary<string, int> questCountByCampaign = definitions
            .GroupBy(d => d.CampaignCode)
            .ToDictionary(g => g.Key, g => g.Count());

        Dictionary<string, int> completedByCampaign = definitions
            .Where(d => progressByQuest.TryGetValue(d.Id, out PlayerQuestEntity? p) && p.Completed)
            .GroupBy(d => d.CampaignCode)
            .ToDictionary(g => g.Key, g => g.Count());

        ImmutableArray<QuestSnapshot>.Builder builder = ImmutableArray.CreateBuilder<QuestSnapshot>(
            definitions.Length
        );

        foreach (QuestDefinitionSnapshot definition in definitions)
        {
            progressByQuest.TryGetValue(definition.Id, out PlayerQuestEntity? progress);
            completedByCampaign.TryGetValue(definition.CampaignCode, out int completedInCampaign);

            builder.Add(
                QuestSnapshotBuilder.Build(
                    definition,
                    progress?.CompletedSteps ?? 0,
                    progress?.Accepted ?? false,
                    progress?.Completed ?? false,
                    completedInCampaign,
                    questCountByCampaign.GetValueOrDefault(definition.CampaignCode),
                    DateTime.Now
                )
            );
        }

        return builder.ToImmutable();
    }

    private async Task<Dictionary<int, PlayerQuestEntity>> LoadProgressAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<PlayerQuestEntity> rows = await dbCtx
                .PlayerQuests.AsNoTracking()
                .Where(p => p.PlayerEntityId == PlayerId)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            return rows.ToDictionary(r => r.QuestEntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load quest progress for player {PlayerId}", PlayerId);
            return [];
        }
    }
}
