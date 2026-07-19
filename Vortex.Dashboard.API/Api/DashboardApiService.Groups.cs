using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Audit;
using Vortex.Database.Entities.Groups;
using Vortex.Primitives.Observability;

namespace Vortex.Dashboard.API.Api;

internal sealed partial class DashboardApiService
{
    /// <summary>Read-only overview of the guilds/forums domain: population, growth, top guilds, and
    /// recent activity pulled from the existing <c>AuditCategory.Social</c> trail (see
    /// <c>Vortex.Observability/Events/GroupAuditHandlers.cs</c>/<c>GroupForumAuditHandlers.cs</c>) —
    /// no new instrumentation needed, the domain already audits itself.</summary>
    public Task<object> GroupsStatsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                DateTime until = ParseDateTime(query["until"]) ?? DateTime.UtcNow;
                DateTime since = ParseDateTime(query["since"]) ?? until.AddDays(-30);
                string granularity = NormalizeGranularity(query["granularity"]);

                int totalGroups = await db
                    .Groups.AsNoTracking()
                    .CountAsync(ct)
                    .ConfigureAwait(false);
                int totalMembers = await db
                    .GroupMembers.AsNoTracking()
                    .CountAsync(ct)
                    .ConfigureAwait(false);
                int totalThreads = await db
                    .GroupForumThreads.AsNoTracking()
                    .CountAsync(ct)
                    .ConfigureAwait(false);
                int totalPosts = await db
                    .GroupForumPosts.AsNoTracking()
                    .CountAsync(ct)
                    .ConfigureAwait(false);

                List<DateTime> createdDates = await db
                    .Groups.AsNoTracking()
                    .Where(g => g.CreatedAt >= since && g.CreatedAt <= until)
                    .Select(g => g.CreatedAt)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<DateTime, int> bucketMap = new();
                DateTime cursor = ResolveCalendarBucket(since, granularity);
                DateTime end = ResolveCalendarBucket(until, granularity);

                while (cursor <= end)
                {
                    bucketMap[cursor] = 0;
                    cursor = NextCalendarBucket(cursor, granularity);
                }

                foreach (DateTime createdAt in createdDates)
                {
                    DateTime bucket = ResolveCalendarBucket(createdAt, granularity);
                    bucketMap[bucket] = bucketMap.GetValueOrDefault(bucket) + 1;
                }

                var growth = bucketMap
                    .OrderBy(pair => pair.Key)
                    .Select(pair => new
                    {
                        bucket = pair.Key.ToString("O"),
                        label = FormatCalendarLabel(pair.Key, granularity),
                        groupsCreated = pair.Value,
                    })
                    .ToList();

                var topByMembers = await db
                    .Groups.AsNoTracking()
                    .Select(g => new
                    {
                        groupId = g.Id,
                        g.Name,
                        g.Badge,
                        ownerId = g.OwnerPlayerEntityId,
                        ownerName = g.OwnerPlayerEntity.Name,
                        memberCount = db.GroupMembers.Count(m => m.GroupEntityId == g.Id),
                        roomId = g.RoomEntityId,
                    })
                    .OrderByDescending(g => g.memberCount)
                    .Take(10)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // GroupBadge isn't SQL-translatable, so the badge URL is attached in a second pass over
                // the materialized rows (same shape as the catalog/furni icon URLs).
                var topByMembersWithBadges = topByMembers
                    .Select(g => new
                    {
                        g.groupId,
                        g.Name,
                        g.Badge,
                        badgeUrl = _assetUrls.GroupBadge(g.Badge),
                        g.ownerId,
                        g.ownerName,
                        g.memberCount,
                        g.roomId,
                    })
                    .ToList();

                var topByForumActivity = await db
                    .Groups.AsNoTracking()
                    .Select(g => new
                    {
                        groupId = g.Id,
                        g.Name,
                        threadCount = db.GroupForumThreads.Count(th => th.GroupEntityId == g.Id),
                        postCount = db.GroupForumPosts.Count(p => p.GroupEntityId == g.Id),
                    })
                    .Where(g => g.threadCount > 0 || g.postCount > 0)
                    .OrderByDescending(g => g.postCount)
                    .Take(10)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var recentActivity = await db
                    .AuditEvents.AsNoTracking()
                    .Where(a =>
                        a.Category == AuditCategory.Social
                        && a.OccurredAt >= since
                        && a.OccurredAt <= until
                    )
                    .OrderByDescending(a => a.OccurredAt)
                    .Take(30)
                    .Select(a => new
                    {
                        a.OccurredAt,
                        a.Action,
                        a.ActorPlayerId,
                        a.Result,
                        a.Data,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<int> actorIds = NormalizeIds(recentActivity.Select(a => a.ActorPlayerId));
                Dictionary<int, string> actorNames = await LoadPlayerNamesAsync(db, actorIds, ct)
                    .ConfigureAwait(false);

                var recentActivityWithNames = recentActivity
                    .Select(a => new
                    {
                        a.OccurredAt,
                        a.Action,
                        actorPlayerId = ToPlayerId(a.ActorPlayerId),
                        actorPlayerName = ResolvePlayerName(actorNames, a.ActorPlayerId),
                        result = a.Result.ToString(),
                        a.Data,
                    })
                    .ToList();

                double avgMembersPerGroup =
                    totalGroups > 0 ? Math.Round((double)totalMembers / totalGroups, 2) : 0d;

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
                        totalGroups,
                        totalMembers,
                        totalThreads,
                        totalPosts,
                        avgMembersPerGroup,
                    },
                    growth,
                    topGroupsByMembers = topByMembersWithBadges,
                    topGroupsByForumActivity = topByForumActivity,
                    recentActivity = recentActivityWithNames,
                };
            },
            ct
        );
}
