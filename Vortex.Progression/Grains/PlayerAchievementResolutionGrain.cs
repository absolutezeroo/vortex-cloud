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
using Vortex.Database.Entities.Furniture;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Primitives.Server.Grains;
using Vortex.Progression.Achievements;
using Vortex.Protocol.Messages.Outgoing.Game.Lobby;

namespace Vortex.Progression.Grains;

/// <summary>
/// The resolution statues a player owns. Orleans serialises per player, so picking a challenge and
/// finishing one cannot interleave — which is what keeps "already challenged" honest.
/// </summary>
internal sealed class PlayerAchievementResolutionGrain(
    IGrainFactory grainFactory,
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<PlayerAchievementResolutionGrain> logger
) : Grain, IPlayerAchievementResolutionGrain
{
    /// <summary>How long a challenge runs. Admin-editable rather than a constant: a hotel running a
    /// Lunar New Year event wants weeks, one testing wants minutes.</summary>
    private const string DurationKey = "achievements.resolution.duration_seconds";

    private const int DefaultDurationSeconds = 7 * 24 * 60 * 60;

    /// <summary>The logic the client binds the statue widget to. A furni that is not one of these
    /// cannot open the dialog, so answering for it would be answering a forged packet.</summary>
    private const string ResolutionLogic = "furniture_achievement_resolution";

    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<PlayerAchievementResolutionGrain> _logger = logger;

    private int PlayerId => (int)this.GetPrimaryKeyLong();

    private IPlayerPresenceGrain Presence => _grainFactory.GetPlayerPresenceGrain((long)PlayerId);

    public async Task OpenAsync(int stuffId, int achievementId, CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            FurnitureEntity? statue = await LoadStatueAsync(dbCtx, stuffId, ct)
                .ConfigureAwait(true);

            if (statue is null)
            {
                return;
            }

            PlayerAchievementResolutionEntity? challenge = await dbCtx
                .PlayerAchievementResolutions.FirstOrDefaultAsync(
                    r => r.ItemEntityId == stuffId,
                    ct
                )
                .ConfigureAwait(true);

            DateTime now = DateTime.UtcNow;

            // A pick only lands when the statue is free. Sending an achievement id at a statue that
            // already carries a live challenge is what the client does after a level-up refresh, so
            // it must not silently start a second one.
            if (
                achievementId > 0
                && (
                    challenge is null
                    || !AchievementResolutionRules.IsInProgress(
                        challenge.CompletedAt,
                        challenge.EndsAt,
                        now
                    )
                )
            )
            {
                challenge = await StartChallengeAsync(
                        dbCtx,
                        statue,
                        challenge,
                        achievementId,
                        now,
                        ct
                    )
                    .ConfigureAwait(true);
            }

            await AnswerAsync(dbCtx, statue, challenge, now, ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to open resolution statue {StuffId} for player {PlayerId}.",
                stuffId,
                PlayerId
            );
        }
    }

    public async Task ResetAsync(int stuffId, CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            FurnitureEntity? statue = await LoadStatueAsync(dbCtx, stuffId, ct)
                .ConfigureAwait(true);

            if (statue is null)
            {
                return;
            }

            PlayerAchievementResolutionEntity? challenge = await dbCtx
                .PlayerAchievementResolutions.FirstOrDefaultAsync(
                    r => r.ItemEntityId == stuffId,
                    ct
                )
                .ConfigureAwait(true);

            // A finished challenge is not reset: its badge is already handed out, and clearing the
            // row would let the same achievement be won twice on the same statue.
            if (
                challenge is null
                || !AchievementResolutionRules.IsInProgress(
                    challenge.CompletedAt,
                    challenge.EndsAt,
                    DateTime.UtcNow
                )
            )
            {
                await AnswerAsync(dbCtx, statue, challenge, DateTime.UtcNow, ct)
                    .ConfigureAwait(true);
                return;
            }

            dbCtx.PlayerAchievementResolutions.Remove(challenge);
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            await AnswerAsync(dbCtx, statue, challenge: null, DateTime.UtcNow, ct)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to reset resolution statue {StuffId} for player {PlayerId}.",
                stuffId,
                PlayerId
            );
        }
    }

    public async Task OnAchievementLevelUpAsync(
        int achievementId,
        int completedLevels,
        CancellationToken ct
    )
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<PlayerAchievementResolutionEntity> live = await dbCtx
                .PlayerAchievementResolutions.Where(r =>
                    r.PlayerEntityId == PlayerId
                    && r.AchievementEntityId == achievementId
                    && r.CompletedAt == null
                )
                .ToListAsync(ct)
                .ConfigureAwait(true);

            if (live.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            AchievementDefinitionSnapshot? definition = await FindDefinitionAsync(achievementId, ct)
                .ConfigureAwait(true);

            foreach (PlayerAchievementResolutionEntity challenge in live)
            {
                if (
                    !AchievementResolutionRules.IsWon(
                        completedLevels,
                        challenge.TargetLevel,
                        challenge.EndsAt,
                        now
                    )
                )
                {
                    continue;
                }

                string badge = BadgeForLevel(definition, challenge.TargetLevel);

                challenge.CompletedAt = now;
                challenge.AwardedBadgeCode = badge;

                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

                if (badge.Length > 0)
                {
                    await _grainFactory
                        .GetInventoryGrain(PlayerId)
                        .GrantBadgeAsync(badge, ct)
                        .ConfigureAwait(true);
                }

                await Presence
                    .SendComposerAsync(
                        new AchievementResolutionCompletedMessageComposer
                        {
                            StuffCode = await LoadStuffCodeAsync(dbCtx, challenge.ItemEntityId, ct)
                                .ConfigureAwait(true),
                            BadgeCode = badge,
                        }
                    )
                    .ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to settle resolutions for player {PlayerId} on achievement {AchievementId}.",
                PlayerId,
                achievementId
            );
        }
    }

    /// <summary>
    /// The statue, if this player really owns an item of that id and it really is a resolution
    /// statue. Both halves matter: the stuff id arrives straight off the wire.
    /// </summary>
    private async Task<FurnitureEntity?> LoadStatueAsync(
        VortexDbContext dbCtx,
        int stuffId,
        CancellationToken ct
    )
    {
        FurnitureEntity? item = await dbCtx
            .Furnitures.AsNoTracking()
            .Include(f => f.FurnitureDefinitionEntity)
            .FirstOrDefaultAsync(f => f.Id == stuffId && f.PlayerEntityId == PlayerId, ct)
            .ConfigureAwait(true);

        return item?.FurnitureDefinitionEntity?.Logic == ResolutionLogic ? item : null;
    }

    private async Task<PlayerAchievementResolutionEntity?> StartChallengeAsync(
        VortexDbContext dbCtx,
        FurnitureEntity statue,
        PlayerAchievementResolutionEntity? previous,
        int achievementId,
        DateTime now,
        CancellationToken ct
    )
    {
        ImmutableArray<AchievementResolutionSnapshot> offers = await BuildOffersAsync(dbCtx, ct)
            .ConfigureAwait(true);

        AchievementResolutionSnapshot? offer = offers.FirstOrDefault(o =>
            o.AchievementId == achievementId
        );

        // Not merely "unknown id": an offer the picker greyed out must be refused here too, or the
        // save button could be replayed to challenge something already finished.
        if (offer is null || offer.State != AchievementResolutionState.Selectable)
        {
            return previous;
        }

        int seconds = await _grainFactory
            .GetServerConfigGrain()
            .GetIntAsync(DurationKey, DefaultDurationSeconds)
            .ConfigureAwait(true);

        if (seconds <= 0)
        {
            seconds = DefaultDurationSeconds;
        }

        // One row per statue: an expired challenge is replaced rather than accumulated, so the
        // unique index on the item holds.
        if (previous is not null)
        {
            dbCtx.PlayerAchievementResolutions.Remove(previous);
        }

        PlayerAchievementResolutionEntity challenge = new()
        {
            PlayerEntityId = PlayerId,
            ItemEntityId = statue.Id,
            AchievementEntityId = achievementId,
            TargetLevel = offer.RequiredLevel,
            StartedAt = now,
            EndsAt = now.AddSeconds(seconds),
        };

        dbCtx.PlayerAchievementResolutions.Add(challenge);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        return challenge;
    }

    /// <summary>
    /// Sends whichever of the three screens matches the statue's state. One place, because the
    /// client asks the same question after opening, picking, levelling up and resetting.
    /// </summary>
    private async Task AnswerAsync(
        VortexDbContext dbCtx,
        FurnitureEntity statue,
        PlayerAchievementResolutionEntity? challenge,
        DateTime now,
        CancellationToken ct
    )
    {
        if (challenge?.CompletedAt is not null)
        {
            await Presence
                .SendComposerAsync(
                    new AchievementResolutionCompletedMessageComposer
                    {
                        StuffCode = statue.FurnitureDefinitionEntity?.Name ?? string.Empty,
                        BadgeCode = challenge.AwardedBadgeCode ?? string.Empty,
                    }
                )
                .ConfigureAwait(true);
            return;
        }

        if (
            challenge is not null
            && AchievementResolutionRules.IsInProgress(challenge.CompletedAt, challenge.EndsAt, now)
        )
        {
            AchievementDefinitionSnapshot? definition = await FindDefinitionAsync(
                    challenge.AchievementEntityId,
                    ct
                )
                .ConfigureAwait(true);

            PlayerAchievementEntity? progress = await dbCtx
                .PlayerAchievements.AsNoTracking()
                .FirstOrDefaultAsync(
                    p =>
                        p.PlayerEntityId == PlayerId
                        && p.AchievementEntityId == challenge.AchievementEntityId,
                    ct
                )
                .ConfigureAwait(true);

            await Presence
                .SendComposerAsync(
                    new AchievementResolutionProgressMessageComposer
                    {
                        StuffId = statue.Id,
                        AchievementId = challenge.AchievementEntityId,
                        RequiredLevelBadgeCode = BadgeForLevel(definition, challenge.TargetLevel),
                        // Levels, not the achievement's own progress counter: the dialog reads
                        // "your progress 2/3" against the target level it named a line above.
                        UserProgress = progress?.Level ?? 0,
                        TotalProgress = challenge.TargetLevel,
                        SecondsLeft = AchievementResolutionRules.SecondsLeft(challenge.EndsAt, now),
                    }
                )
                .ConfigureAwait(true);
            return;
        }

        ImmutableArray<AchievementResolutionSnapshot> offers = await BuildOffersAsync(dbCtx, ct)
            .ConfigureAwait(true);

        int durationSeconds = await _grainFactory
            .GetServerConfigGrain()
            .GetIntAsync(DurationKey, DefaultDurationSeconds)
            .ConfigureAwait(true);

        await Presence
            .SendComposerAsync(
                new AchievementResolutionsMessageComposer
                {
                    StuffId = statue.Id,
                    Achievements = offers,
                    // Nothing has started yet, so the countdown shows what the player would get,
                    // not a remainder.
                    SecondsLeft = durationSeconds > 0 ? durationSeconds : DefaultDurationSeconds,
                }
            )
            .ConfigureAwait(true);
    }

    private async Task<ImmutableArray<AchievementResolutionSnapshot>> BuildOffersAsync(
        VortexDbContext dbCtx,
        CancellationToken ct
    )
    {
        List<AchievementResolutionEntity> offers = await dbCtx
            .AchievementResolutions.AsNoTracking()
            .Where(o => o.Enabled)
            .OrderBy(o => o.SortOrder)
            .ThenBy(o => o.Id)
            .ToListAsync(ct)
            .ConfigureAwait(true);

        if (offers.Count == 0)
        {
            return [];
        }

        Dictionary<int, int> completedByAchievement = await dbCtx
            .PlayerAchievements.AsNoTracking()
            .Where(p => p.PlayerEntityId == PlayerId)
            .ToDictionaryAsync(p => p.AchievementEntityId, p => p.Level, ct)
            .ConfigureAwait(true);

        DateTime now = DateTime.UtcNow;

        HashSet<int> challenged =
        [
            .. (
                await dbCtx
                    .PlayerAchievementResolutions.AsNoTracking()
                    .Where(r => r.PlayerEntityId == PlayerId && r.CompletedAt == null)
                    .ToListAsync(ct)
                    .ConfigureAwait(true)
            )
                .Where(r => r.EndsAt > now)
                .Select(r => r.AchievementEntityId),
        ];

        ImmutableArray<AchievementDefinitionSnapshot> definitions = await _grainFactory
            .GetAchievementManagerGrain()
            .GetDefinitionsAsync(ct)
            .ConfigureAwait(true);

        Dictionary<int, AchievementDefinitionSnapshot> definitionsById = definitions.ToDictionary(
            d => d.Id
        );

        ImmutableArray<AchievementResolutionSnapshot>.Builder built =
            ImmutableArray.CreateBuilder<AchievementResolutionSnapshot>(offers.Count);

        foreach (AchievementResolutionEntity offer in offers)
        {
            if (
                !definitionsById.TryGetValue(
                    offer.AchievementEntityId,
                    out AchievementDefinitionSnapshot? definition
                )
            )
            {
                // An offer pointing at an achievement that no longer exists is dropped rather than
                // sent with an empty badge: the dialog would show a blank, unpickable row.
                continue;
            }

            int completed = completedByAchievement.GetValueOrDefault(offer.AchievementEntityId);
            int levelCount = definition.Levels.Length;

            int target = AchievementResolutionRules.ResolveTargetLevel(
                completed,
                levelCount,
                offer.TargetLevelOffset
            );

            built.Add(
                new AchievementResolutionSnapshot
                {
                    AchievementId = offer.AchievementEntityId,
                    Level = completed,
                    BadgeId = BadgeForLevel(definition, target),
                    RequiredLevel = target,
                    State = (AchievementResolutionState)
                        AchievementResolutionRules.ResolveState(
                            completed,
                            levelCount,
                            challenged.Contains(offer.AchievementEntityId)
                        ),
                }
            );
        }

        return built.ToImmutable();
    }

    private async Task<AchievementDefinitionSnapshot?> FindDefinitionAsync(
        int achievementId,
        CancellationToken ct
    )
    {
        ImmutableArray<AchievementDefinitionSnapshot> definitions = await _grainFactory
            .GetAchievementManagerGrain()
            .GetDefinitionsAsync(ct)
            .ConfigureAwait(true);

        return definitions.FirstOrDefault(d => d.Id == achievementId);
    }

    private static string BadgeForLevel(AchievementDefinitionSnapshot? definition, int level)
    {
        if (definition is null)
        {
            return string.Empty;
        }

        foreach (AchievementLevelSnapshot candidate in definition.Levels)
        {
            if (candidate.Level == level)
            {
                return candidate.BadgeCode;
            }
        }

        return string.Empty;
    }

    private static async Task<string> LoadStuffCodeAsync(
        VortexDbContext dbCtx,
        int itemId,
        CancellationToken ct
    )
    {
        FurnitureEntity? item = await dbCtx
            .Furnitures.AsNoTracking()
            .Include(f => f.FurnitureDefinitionEntity)
            .FirstOrDefaultAsync(f => f.Id == itemId, ct)
            .ConfigureAwait(true);

        return item?.FurnitureDefinitionEntity?.Name ?? string.Empty;
    }
}
