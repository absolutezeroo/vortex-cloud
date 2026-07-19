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
using Vortex.Database.Entities.Achievements;
using Vortex.Players.Achievements;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Messages.Outgoing.Inventory.Achievements;
using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.Players.Grains;

/// <summary>
/// Per-player achievement grain. Resolves the player's stored progress rows against the cached
/// definitions into wire-ready snapshots (reads) and applies progression end to end (writes): DB
/// upsert, badge replacement, currency rewards, achievement-score update and outbound composers.
/// Orleans single-threading serialises progression per player, so no locking is needed.
/// </summary>
internal sealed class PlayerAchievementGrain(
    IGrainFactory grainFactory,
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<PlayerAchievementGrain> logger
) : Grain, IPlayerAchievementGrain
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<PlayerAchievementGrain> _logger = logger;

    private int PlayerId => (int)this.GetPrimaryKeyLong();

    public async Task<AchievementListSnapshot> GetAchievementsAsync(CancellationToken ct)
    {
        IAchievementManagerGrain manager = _grainFactory.GetAchievementManagerGrain();

        ImmutableArray<AchievementDefinitionSnapshot> definitions = await manager
            .GetDefinitionsAsync(ct)
            .ConfigureAwait(true);

        if (definitions.IsEmpty)
        {
            return new AchievementListSnapshot
            {
                Achievements = ImmutableArray<AchievementProgressSnapshot>.Empty,
                DefaultCategory = string.Empty,
                Score = 0,
            };
        }

        Dictionary<int, PlayerAchievementEntity> progressByAchievement = await LoadProgressAsync(ct)
            .ConfigureAwait(true);

        ImmutableArray<AchievementProgressSnapshot>.Builder builder =
            ImmutableArray.CreateBuilder<AchievementProgressSnapshot>(definitions.Length);
        int score = 0;

        foreach (AchievementDefinitionSnapshot definition in definitions)
        {
            progressByAchievement.TryGetValue(definition.Id, out PlayerAchievementEntity? progress);

            int cumulativeProgress = progress?.Progress ?? 0;
            int completedLevels = progress?.Level ?? 0;

            builder.Add(
                AchievementProgressCalculator.Build(definition, cumulativeProgress, completedLevels)
            );
            score += AchievementProgressCalculator.ComputeScore(definition, completedLevels);
        }

        string defaultCategory = await manager.GetDefaultCategoryAsync(ct).ConfigureAwait(true);

        return new AchievementListSnapshot
        {
            Achievements = builder.ToImmutable(),
            DefaultCategory = defaultCategory,
            Score = score,
        };
    }

    public Task ProgressAsync(string achievementName, int amount, CancellationToken ct) =>
        ProgressCoreAsync(achievementName, amount, oncePerDay: false, ct);

    public Task ProgressDailyAsync(string achievementName, int amount, CancellationToken ct) =>
        ProgressCoreAsync(achievementName, amount, oncePerDay: true, ct);

    private async Task ProgressCoreAsync(
        string achievementName,
        int amount,
        bool oncePerDay,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(achievementName) || amount <= 0)
        {
            return;
        }

        AchievementDefinitionSnapshot? definition = await _grainFactory
            .GetAchievementManagerGrain()
            .GetByNameAsync(achievementName, ct)
            .ConfigureAwait(true);

        if (definition is null)
        {
            _logger.LogWarning(
                "Progress requested for unknown achievement '{Name}' (player {PlayerId})",
                achievementName,
                PlayerId
            );
            return;
        }

        int oldCompleted;
        AchievementProgressResult result;

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            PlayerAchievementEntity? row = await dbCtx
                .PlayerAchievements.FirstOrDefaultAsync(
                    p => p.PlayerEntityId == PlayerId && p.AchievementEntityId == definition.Id,
                    ct
                )
                .ConfigureAwait(true);

            // Daily achievements (e.g. Login) advance at most once per calendar day: the row's
            // last-write date is when it last progressed, so skip if that was already today.
            if (oncePerDay && row is not null && row.UpdatedAt.Date == DateTime.Now.Date)
            {
                return;
            }

            oldCompleted = row?.Level ?? 0;

            result = AchievementProgressCalculator.ApplyProgress(
                definition,
                row?.Progress ?? 0,
                oldCompleted,
                amount
            );

            if (!result.ProgressChanged)
            {
                return;
            }

            if (row is null)
            {
                dbCtx.PlayerAchievements.Add(
                    new PlayerAchievementEntity
                    {
                        PlayerEntityId = PlayerId,
                        AchievementEntityId = definition.Id,
                        Progress = result.NewProgress,
                        Level = result.NewCompletedLevels,
                    }
                );
            }
            else
            {
                row.Progress = result.NewProgress;
                row.Level = result.NewCompletedLevels;
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to persist achievement progress for player {PlayerId}, achievement {Name}",
                PlayerId,
                definition.Name
            );
            return;
        }

        IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain((long)PlayerId);

        if (result.LeveledUp)
        {
            await ApplyLevelUpsAsync(definition, oldCompleted, result, presence, ct)
                .ConfigureAwait(true);
        }

        // Always refresh this achievement's row so an open achievements window updates live.
        await presence
            .SendComposerAsync(
                new AchievementEventMessageComposer
                {
                    Achievement = AchievementProgressCalculator.Build(
                        definition,
                        result.NewProgress,
                        result.NewCompletedLevels
                    ),
                }
            )
            .ConfigureAwait(true);
    }

    private async Task ApplyLevelUpsAsync(
        AchievementDefinitionSnapshot definition,
        int oldCompleted,
        AchievementProgressResult result,
        IPlayerPresenceGrain presence,
        CancellationToken ct
    )
    {
        IInventoryGrain inventory = _grainFactory.GetInventoryGrain((long)PlayerId);
        IPlayerWalletGrain wallet = _grainFactory.GetPlayerWalletGrain((long)PlayerId);

        string badgeHeldBefore =
            oldCompleted >= 1 ? definition.Levels[oldCompleted - 1].BadgeCode : string.Empty;
        string finalBadge = definition.Levels[result.NewCompletedLevels - 1].BadgeCode;

        // A player holds only the highest achievement badge: drop the previous one, grant the new.
        if (!string.IsNullOrEmpty(badgeHeldBefore) && badgeHeldBefore != finalBadge)
        {
            await inventory.RemoveBadgeAsync(badgeHeldBefore, ct).ConfigureAwait(true);
        }

        await inventory.GrantBadgeAsync(finalBadge, ct).ConfigureAwait(true);

        int scoreGained = 0;
        foreach (AchievementLevelUp levelUp in result.LevelUps)
        {
            scoreGained += levelUp.ScorePoints;

            if (levelUp.RewardAmount <= 0)
            {
                continue;
            }

            if (levelUp.RewardType < 0)
            {
                await wallet.GrantCreditsAsync(levelUp.RewardAmount, ct).ConfigureAwait(true);
            }
            else
            {
                await wallet
                    .GrantActivityPointsAsync(levelUp.RewardType, levelUp.RewardAmount, ct)
                    .ConfigureAwait(true);
            }
        }

        int newScore = await _grainFactory
            .GetGrain<IPlayerGrain>((long)PlayerId)
            .AddAchievementScoreAsync(scoreGained, ct)
            .ConfigureAwait(true);

        AchievementLevelUp lastLevel = result.LevelUps[^1];

        await presence
            .SendComposerAsync(
                new HabboAchievementNotificationMessageComposer
                {
                    Type = 0,
                    Level = result.NewCompletedLevels,
                    BadgeId = 0,
                    BadgeCode = finalBadge,
                    Points = scoreGained,
                    LevelRewardPoints = lastLevel.RewardAmount,
                    LevelRewardPointType = lastLevel.RewardType,
                    BonusPoints = 0,
                    AchievementId = definition.Id,
                    RemovedBadgeCode = badgeHeldBefore,
                    Category = definition.Category,
                    ShowDialogToUser = true,
                    OwnerCount = 0,
                    BadgeRarityId = 0,
                }
            )
            .ConfigureAwait(true);

        await presence
            .SendComposerAsync(new AchievementsScoreEventMessageComposer { Score = newScore })
            .ConfigureAwait(true);
    }

    private async Task<Dictionary<int, PlayerAchievementEntity>> LoadProgressAsync(
        CancellationToken ct
    )
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<PlayerAchievementEntity> rows = await dbCtx
                .PlayerAchievements.AsNoTracking()
                .Where(p => p.PlayerEntityId == PlayerId)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            return rows.ToDictionary(r => r.AchievementEntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load achievement progress for player {PlayerId}",
                PlayerId
            );
            return [];
        }
    }
}
