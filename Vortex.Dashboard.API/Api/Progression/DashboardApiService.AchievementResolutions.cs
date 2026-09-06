using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Achievements;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// The resolution statues: what they offer, and what players are actually doing with them.
/// <para>
/// The raw tables cannot answer the two questions an operator has. <c>achievement_resolutions</c>
/// says an offer exists but not whether the achievement behind it still does — a row pointing at a
/// deleted definition is silently dropped from the picker rather than shown broken, so it goes
/// missing without a trace. And <c>player_achievement_resolutions</c> has no "expired" column: a
/// challenge is over because a date passed, with nothing having run at the moment it did, so the
/// live/expired split has to be computed here or it does not exist anywhere.
/// </para>
/// </summary>
internal sealed partial class DashboardApiService
{
    /// <summary>Offers, plus a completion rate per offer. Optional <c>state</c> filter over the
    /// challenges list: <c>live</c>, <c>completed</c> or <c>expired</c>.</summary>
    public Task<object> AchievementResolutionsAsync(
        NameValueCollection query,
        CancellationToken ct
    ) =>
        QueryAsync<object>(
            async db =>
            {
                string state = (query["state"] ?? string.Empty).Trim().ToLowerInvariant();
                DateTime now = DateTime.UtcNow;

                var offers = await db
                    .AchievementResolutions.AsNoTracking()
                    .OrderBy(o => o.SortOrder)
                    .ThenBy(o => o.Id)
                    .Select(o => new
                    {
                        o.Id,
                        o.AchievementEntityId,
                        o.TargetLevelOffset,
                        o.SortOrder,
                        o.Enabled,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<int> achievementIds = offers.Select(o => o.AchievementEntityId).ToList();

                Dictionary<int, AchievementEntity> definitions = await db
                    .Achievements.AsNoTracking()
                    .Where(a => achievementIds.Contains(a.Id))
                    .ToDictionaryAsync(a => a.Id, ct)
                    .ConfigureAwait(false);

                Dictionary<int, int> levelCounts = (
                    await db
                        .AchievementLevels.AsNoTracking()
                        .Where(l => achievementIds.Contains(l.AchievementEntityId))
                        .GroupBy(l => l.AchievementEntityId)
                        .Select(g => new { id = g.Key, levels = g.Count() })
                        .ToListAsync(ct)
                        .ConfigureAwait(false)
                ).ToDictionary(g => g.id, g => g.levels);

                List<ResolutionChallengeRow> challenges = await db
                    .PlayerAchievementResolutions.AsNoTracking()
                    .Select(r => new ResolutionChallengeRow(
                        r.Id,
                        r.PlayerEntityId,
                        r.ItemEntityId,
                        r.AchievementEntityId,
                        r.TargetLevel,
                        r.StartedAt,
                        r.EndsAt,
                        r.CompletedAt,
                        r.AwardedBadgeCode
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, ResolutionTally> tallyByAchievement = challenges
                    .GroupBy(c => c.AchievementId)
                    .ToDictionary(
                        g => g.Key,
                        g => new ResolutionTally(
                            g.Count(),
                            g.Count(c => c.CompletedAt is not null),
                            g.Count(c => c.CompletedAt is null && c.EndsAt > now),
                            g.Count(c => c.CompletedAt is null && c.EndsAt <= now)
                        )
                    );

                var offerRows = offers
                    .Select(o =>
                    {
                        AchievementEntity? definition = definitions.GetValueOrDefault(
                            o.AchievementEntityId
                        );
                        ResolutionTally? tally = tallyByAchievement.GetValueOrDefault(
                            o.AchievementEntityId
                        );
                        int taken = tally?.Taken ?? 0;

                        return new
                        {
                            o.Id,
                            achievementId = o.AchievementEntityId,
                            achievementName = definition?.Name,
                            category = definition?.Category,
                            // The one thing the table cannot say: this offer never reaches the
                            // picker, because the grain drops rows whose definition is gone.
                            orphaned = definition is null,
                            levelCount = levelCounts.GetValueOrDefault(o.AchievementEntityId),
                            o.TargetLevelOffset,
                            o.SortOrder,
                            o.Enabled,
                            taken,
                            completed = tally?.Completed ?? 0,
                            live = tally?.Live ?? 0,
                            expired = tally?.Expired ?? 0,
                            completionRate = taken == 0
                                ? 0d
                                : Math.Round((tally?.Completed ?? 0) * 100d / taken, 1),
                        };
                    })
                    .ToList();

                IEnumerable<ResolutionChallengeRow> filtered = state switch
                {
                    "live" => challenges.Where(c => c.CompletedAt is null && c.EndsAt > now),
                    "completed" => challenges.Where(c => c.CompletedAt is not null),
                    "expired" => challenges.Where(c => c.CompletedAt is null && c.EndsAt <= now),
                    _ => challenges,
                };

                List<ResolutionChallengeRow> page = filtered
                    .OrderByDescending(c => c.StartedAt)
                    .Take(200)
                    .ToList();

                Dictionary<int, string> playerNames = await LoadPlayerNamesAsync(
                        db,
                        NormalizeIds(page.Select(c => (int?)c.PlayerId)),
                        ct
                    )
                    .ConfigureAwait(false);

                Dictionary<(int Player, int Achievement), int> reachedByPlayer =
                    await LoadReachedLevelsAsync(db, page, ct).ConfigureAwait(false);

                var challengeRows = page.Select(c => new
                    {
                        c.Id,
                        playerId = c.PlayerId,
                        playerName = ResolvePlayerName(playerNames, c.PlayerId),
                        itemId = c.ItemId,
                        achievementId = c.AchievementId,
                        achievementName = definitions.GetValueOrDefault(c.AchievementId)?.Name,
                        c.TargetLevel,
                        reachedLevel = reachedByPlayer.GetValueOrDefault(
                            (c.PlayerId, c.AchievementId)
                        ),
                        c.StartedAt,
                        c.EndsAt,
                        c.CompletedAt,
                        badgeCode = c.AwardedBadgeCode,
                        badgeUrl = string.IsNullOrEmpty(c.AwardedBadgeCode)
                            ? null
                            : _assetUrls.BadgeImage(c.AwardedBadgeCode),
                        state = c.CompletedAt is not null ? "completed"
                        : c.EndsAt > now ? "live"
                        : "expired",
                    })
                    .ToList();

                int totalCompleted = challenges.Count(c => c.CompletedAt is not null);

                return new
                {
                    offers = offerRows,
                    challenges = challengeRows,
                    totals = new
                    {
                        offers = offerRows.Count,
                        enabledOffers = offerRows.Count(o => o.Enabled),
                        orphanedOffers = offerRows.Count(o => o.orphaned),
                        taken = challenges.Count,
                        completed = totalCompleted,
                        live = challenges.Count(c => c.CompletedAt is null && c.EndsAt > now),
                        expired = challenges.Count(c => c.CompletedAt is null && c.EndsAt <= now),
                        completionRate = challenges.Count == 0
                            ? 0d
                            : Math.Round(totalCompleted * 100d / challenges.Count, 1),
                        // Distinct players, not rows: one player can own several statues.
                        players = challenges.Select(c => c.PlayerId).Distinct().Count(),
                    },
                    truncated = filtered.Count() > page.Count,
                };
            },
            ct
        );

    /// <summary>
    /// How far each challenger has actually got, so the list can show 2/3 rather than just the
    /// target. One pass keyed on (player, achievement) — a per-row lookup would be one query per
    /// challenge.
    /// </summary>
    private static async Task<
        Dictionary<(int Player, int Achievement), int>
    > LoadReachedLevelsAsync(
        Database.Context.VortexDbContext db,
        List<ResolutionChallengeRow> page,
        CancellationToken ct
    )
    {
        if (page.Count == 0)
        {
            return [];
        }

        List<int> playerIds = page.Select(c => c.PlayerId).Distinct().ToList();
        List<int> achievementIds = page.Select(c => c.AchievementId).Distinct().ToList();

        var rows = await db
            .PlayerAchievements.AsNoTracking()
            .Where(p =>
                playerIds.Contains(p.PlayerEntityId)
                && achievementIds.Contains(p.AchievementEntityId)
            )
            .Select(p => new
            {
                p.PlayerEntityId,
                p.AchievementEntityId,
                p.Level,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        Dictionary<(int, int), int> reached = new(rows.Count);

        foreach (var row in rows)
        {
            reached[(row.PlayerEntityId, row.AchievementEntityId)] = row.Level;
        }

        return reached;
    }

    private sealed record ResolutionChallengeRow(
        int Id,
        int PlayerId,
        int ItemId,
        int AchievementId,
        int TargetLevel,
        DateTime StartedAt,
        DateTime EndsAt,
        DateTime? CompletedAt,
        string? AwardedBadgeCode
    );

    private sealed record ResolutionTally(int Taken, int Completed, int Live, int Expired);
}
