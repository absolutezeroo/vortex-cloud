using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Quests;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Read + analytics surface for quests. The admin CRUD lives in
/// <c>DashboardOperationsService.Quests.cs</c>; here we only read. Completion analytics are
/// aggregated from the <c>player_quests</c> table (there is no separate quest-completion audit
/// trail), keyed on each completed row's <c>CompletedAt</c>.
/// </summary>
internal sealed partial class DashboardApiService
{
    // Objective types that have a live progression trigger wired (a handler calls ProgressAsync with
    // this type). The others in QuestTypes are defined but not yet fired, so the admin can still pick
    // them but they won't advance until a trigger exists -- surfaced via the "wired" flag.
    private static readonly HashSet<string> WiredQuestTypes = new(StringComparer.Ordinal)
    {
        Vortex.Primitives.Quests.QuestTypes.RoomEntry,
        Vortex.Primitives.Quests.QuestTypes.FriendListSize,
        Vortex.Primitives.Quests.QuestTypes.AvatarLooks,
        Vortex.Primitives.Quests.QuestTypes.Chat,
        Vortex.Primitives.Quests.QuestTypes.Wave,
        Vortex.Primitives.Quests.QuestTypes.Dance,
        Vortex.Primitives.Quests.QuestTypes.RespectGiven,
        Vortex.Primitives.Quests.QuestTypes.CatalogPurchase,
        Vortex.Primitives.Quests.QuestTypes.Login,
        Vortex.Primitives.Quests.QuestTypes.MottoChange,
        Vortex.Primitives.Quests.QuestTypes.RespectReceived,
        Vortex.Primitives.Quests.QuestTypes.TradeCompleted,
        Vortex.Primitives.Quests.QuestTypes.CreateGroup,
        Vortex.Primitives.Quests.QuestTypes.JoinGroup,
        Vortex.Primitives.Quests.QuestTypes.BuyClub,
        Vortex.Primitives.Quests.QuestTypes.PlaceItem,
    };

    /// <summary>
    /// The canonical objective types (the <c>quest_type</c> values), read by reflection from
    /// <see cref="Vortex.Primitives.Quests.QuestTypes"/> so the admin picks a real objective instead
    /// of typing a free string. <c>wired</c> marks the ones that actually advance today; the quest's
    /// step count (<c>TotalSteps</c>) is the goal (e.g. RoomEntry + 200 = "visit 200 rooms").
    /// </summary>
    public object QuestTypeOptions()
    {
        List<object> items = typeof(Vortex.Primitives.Quests.QuestTypes)
            .GetFields(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
            )
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => (object)new { name, wired = WiredQuestTypes.Contains(name) })
            .ToList();

        return new { count = items.Count, items };
    }

    /// <summary>Every quest with its reward config, timer, and lifetime accept/complete counts,
    /// ordered like the client sees them (sort order then id).</summary>
    public Task<object> QuestsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                string campaign = (query["campaign"] ?? string.Empty).Trim();

                IQueryable<QuestEntity> questsQuery = db.Quests.AsNoTracking();
                if (campaign.Length > 0)
                {
                    questsQuery = questsQuery.Where(q => q.CampaignCode == campaign);
                }

                var rows = await questsQuery
                    .OrderBy(q => q.SortOrder)
                    .ThenBy(q => q.Id)
                    .Select(q => new
                    {
                        q.Id,
                        q.CampaignCode,
                        q.ChainCode,
                        q.LocalizationCode,
                        q.QuestType,
                        q.TargetType,
                        q.TargetValue,
                        q.Enabled,
                        q.TotalSteps,
                        q.RewardType,
                        q.RewardAmount,
                        q.SortOrder,
                        q.Easy,
                        q.Seasonal,
                        q.SeasonalSeconds,
                        q.EndsAt,
                        acceptedCount = db.PlayerQuests.Count(p =>
                            p.QuestEntityId == q.Id && p.Accepted
                        ),
                        completedCount = db.PlayerQuests.Count(p =>
                            p.QuestEntityId == q.Id && p.Completed
                        ),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                DateTime now = DateTime.UtcNow;
                var items = rows.Select(q => new
                    {
                        q.Id,
                        q.CampaignCode,
                        q.ChainCode,
                        q.LocalizationCode,
                        q.QuestType,
                        q.TargetType,
                        q.TargetValue,
                        q.Enabled,
                        q.TotalSteps,
                        q.RewardType,
                        q.RewardAmount,
                        rewardKind = q.RewardType < 0 ? "credits" : "activityPoints",
                        q.SortOrder,
                        q.Easy,
                        q.Seasonal,
                        q.SeasonalSeconds,
                        q.EndsAt,
                        expired = q.Seasonal && q.EndsAt is { } endsAt && endsAt <= now,
                        q.acceptedCount,
                        q.completedCount,
                    })
                    .ToList();

                List<string> campaigns = rows.Select(q => q.CampaignCode)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                return new
                {
                    count = items.Count,
                    campaigns,
                    items,
                };
            },
            ct
        );

    /// <summary>One quest with its full field set plus its lifetime accept/complete totals.</summary>
    public Task<object?> QuestDetailAsync(int questId, CancellationToken ct) =>
        QueryAsync<object?>(
            async db =>
            {
                QuestEntity? quest = await db
                    .Quests.AsNoTracking()
                    .FirstOrDefaultAsync(q => q.Id == questId, ct)
                    .ConfigureAwait(false);

                if (quest is null)
                {
                    return null;
                }

                int acceptedCount = await db
                    .PlayerQuests.AsNoTracking()
                    .CountAsync(p => p.QuestEntityId == questId && p.Accepted, ct)
                    .ConfigureAwait(false);

                int completedCount = await db
                    .PlayerQuests.AsNoTracking()
                    .CountAsync(p => p.QuestEntityId == questId && p.Completed, ct)
                    .ConfigureAwait(false);

                DateTime now = DateTime.UtcNow;
                return new
                {
                    quest.Id,
                    quest.CampaignCode,
                    quest.ChainCode,
                    quest.LocalizationCode,
                    quest.QuestType,
                    quest.TargetType,
                    quest.TargetValue,
                    quest.Enabled,
                    quest.TotalSteps,
                    quest.RewardType,
                    quest.RewardAmount,
                    rewardKind = quest.RewardType < 0 ? "credits" : "activityPoints",
                    quest.CatalogPageName,
                    quest.ImageVersion,
                    quest.SortOrder,
                    quest.Easy,
                    quest.Seasonal,
                    quest.SeasonalSeconds,
                    quest.EndsAt,
                    expired = quest.Seasonal && quest.EndsAt is { } endsAt && endsAt <= now,
                    acceptedCount,
                    completedCount,
                };
            },
            ct
        );

    /// <summary>Quest completions over time and the most-completed quests, aggregated from
    /// <c>player_quests</c>.</summary>
    public Task<object> QuestsStatsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                DateTime until = ParseDateTime(query["until"]) ?? DateTime.UtcNow;
                DateTime since = ParseDateTime(query["since"]) ?? until.AddDays(-30);
                string granularity = NormalizeGranularity(query["granularity"]);

                List<(int QuestId, DateTime CompletedAt)> completions = await db
                    .PlayerQuests.AsNoTracking()
                    .Where(p =>
                        p.Completed
                        && p.CompletedAt != null
                        && p.CompletedAt >= since
                        && p.CompletedAt <= until
                    )
                    .Select(p => new ValueTuple<int, DateTime>(
                        p.QuestEntityId,
                        p.CompletedAt!.Value
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                int totalCompletions = completions.Count;
                int totalAccepted = await db
                    .PlayerQuests.AsNoTracking()
                    .CountAsync(p => p.Accepted, ct)
                    .ConfigureAwait(false);
                int activePlayers = await db
                    .PlayerQuests.AsNoTracking()
                    .Where(p => p.Accepted && !p.Completed)
                    .Select(p => p.PlayerEntityId)
                    .Distinct()
                    .CountAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<DateTime, int> bucketMap = new();
                DateTime cursor = ResolveCalendarBucket(since, granularity);
                DateTime end = ResolveCalendarBucket(until, granularity);

                while (cursor <= end)
                {
                    bucketMap[cursor] = 0;
                    cursor = NextCalendarBucket(cursor, granularity);
                }

                foreach ((int _, DateTime completedAt) in completions)
                {
                    DateTime bucket = ResolveCalendarBucket(completedAt, granularity);
                    bucketMap[bucket] = bucketMap.GetValueOrDefault(bucket) + 1;
                }

                var timeline = bucketMap
                    .OrderBy(pair => pair.Key)
                    .Select(pair => new
                    {
                        bucket = pair.Key.ToString("O"),
                        label = FormatCalendarLabel(pair.Key, granularity),
                        completions = pair.Value,
                    })
                    .ToList();

                List<int> topQuestIds = completions
                    .GroupBy(c => c.QuestId)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => g.Key)
                    .ToList();

                Dictionary<int, int> completionsByQuest = completions
                    .GroupBy(c => c.QuestId)
                    .ToDictionary(g => g.Key, g => g.Count());

                Dictionary<int, string> questNames = await db
                    .Quests.AsNoTracking()
                    .Where(q => topQuestIds.Contains(q.Id))
                    .ToDictionaryAsync(
                        q => q.Id,
                        q => q.CampaignCode + "." + q.LocalizationCode,
                        ct
                    )
                    .ConfigureAwait(false);

                var topQuests = topQuestIds
                    .Select(id => new
                    {
                        questId = id,
                        name = questNames.GetValueOrDefault(id, $"quest #{id}"),
                        completions = completionsByQuest.GetValueOrDefault(id),
                    })
                    .ToList();

                return new
                {
                    window = new
                    {
                        since,
                        until,
                        granularity,
                    },
                    totals = new
                    {
                        totalCompletions,
                        totalAccepted,
                        activePlayers,
                    },
                    timeline,
                    topQuests,
                };
            },
            ct
        );
}
