using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Dashboard.API.Api.Hotel.Contracts;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Database.Context;
using Vortex.Database.Entities.Audit;
using Vortex.Database.Entities.Groups;
using Vortex.Primitives.Observability;

namespace Vortex.Dashboard.API.Api.Hotel;

internal sealed class GroupReads(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    DashboardAssetUrls assetUrls
) : DashboardReads(dbContextFactory)
{
    private readonly DashboardAssetUrls _assetUrls = assetUrls;

    /// <summary>Read-only overview of the guilds/forums domain: population, growth, top guilds, and
    /// recent activity pulled from the existing <c>AuditCategory.Social</c> trail (see
    /// <c>Vortex.Observability/Events/GroupAuditHandlers.cs</c>/<c>GroupForumAuditHandlers.cs</c>) —
    /// no new instrumentation needed, the domain already audits itself.</summary>
    public Task<GroupStats> GroupsStatsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<GroupStats>(
            async db =>
            {
                (DateTime since, DateTime until) = TimeWindow.Resolve(query, DateTime.UtcNow);
                string granularity = TimeWindow.Granularity(query["granularity"]);

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
                DateTime cursor = TimeWindow.Bucket(since, granularity);
                DateTime end = TimeWindow.Bucket(until, granularity);

                while (cursor <= end)
                {
                    bucketMap[cursor] = 0;
                    cursor = TimeWindow.NextBucket(cursor, granularity);
                }

                foreach (DateTime createdAt in createdDates)
                {
                    DateTime bucket = TimeWindow.Bucket(createdAt, granularity);
                    bucketMap[bucket] = bucketMap.GetValueOrDefault(bucket) + 1;
                }

                List<GroupGrowthPoint> growth = bucketMap
                    .OrderBy(pair => pair.Key)
                    .Select(pair => new GroupGrowthPoint(
                        pair.Key.ToString("O"),
                        TimeWindow.Label(pair.Key, granularity),
                        pair.Value
                    ))
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
                List<GroupMemberRanking> topByMembersWithBadges = topByMembers
                    .Select(g => new GroupMemberRanking(
                        g.groupId,
                        g.Name,
                        g.Badge,
                        _assetUrls.GroupBadge(g.Badge),
                        g.ownerId,
                        g.ownerName,
                        g.memberCount,
                        g.roomId
                    ))
                    .ToList();

                // Projected into an anonymous type and mapped afterwards, which is not a style
                // preference: filtering or ordering AFTER a projection into a positional record is
                // untranslatable. EF keeps the member bindings of an anonymous type and can push
                // `.Where(x => x.threadCount > 0)` back down onto the subquery it came from; a
                // record built through a constructor is opaque to it, so the same clause threw
                // "could not be translated" and the endpoint answered 500 for every caller.
                //
                // Nothing offline catches this: the in-memory provider used by the tests is LINQ to
                // objects and translates everything happily. It fails against MySQL and only there.
                var forumActivity = await db
                    .Groups.AsNoTracking()
                    .Select(g => new
                    {
                        g.Id,
                        g.Name,
                        threadCount = db.GroupForumThreads.Count(th => th.GroupEntityId == g.Id),
                        postCount = db.GroupForumPosts.Count(p => p.GroupEntityId == g.Id),
                    })
                    .Where(g => g.threadCount > 0 || g.postCount > 0)
                    .OrderByDescending(g => g.postCount)
                    .Take(10)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<GroupForumRanking> topByForumActivity =
                [
                    .. forumActivity.Select(g => new GroupForumRanking(
                        g.Id,
                        g.Name,
                        g.threadCount,
                        g.postCount
                    )),
                ];

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

                List<int> actorIds = DisplayNameQueries.NormalizeIds(
                    recentActivity.Select(a => a.ActorPlayerId)
                );
                Dictionary<int, string> actorNames = await db.PlayerNamesAsync(actorIds, ct)
                    .ConfigureAwait(false);

                List<GroupActivityEvent> recentActivityWithNames = recentActivity
                    .Select(a => new GroupActivityEvent(
                        a.OccurredAt,
                        a.Action,
                        DisplayNameQueries.ToPlayerId(a.ActorPlayerId),
                        DisplayNameQueries.ResolvePlayerName(actorNames, a.ActorPlayerId),
                        a.Result.ToString(),
                        a.Data
                    ))
                    .ToList();

                double avgMembersPerGroup =
                    totalGroups > 0 ? Math.Round((double)totalMembers / totalGroups, 2) : 0d;

                return new GroupStats(
                    new ReportWindow(since, until, granularity),
                    new GroupTotals(
                        totalGroups,
                        totalMembers,
                        totalThreads,
                        totalPosts,
                        avgMembersPerGroup
                    ),
                    growth,
                    topByMembersWithBadges,
                    topByForumActivity,
                    recentActivityWithNames
                );
            },
            ct
        );
}
