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

namespace Vortex.Dashboard.API.Api.Hotel;

/// <summary>
/// The social graph and the guild forums: friendships, pending requests, the blocking/ignoring that
/// is invisible from any other surface, private-message volume, and forum activity.
/// <para>
/// <c>messenger_friends</c> holds one row per direction (both are written on accept), so a raw row
/// count double-counts every friendship — the totals here halve it and say so.
/// </para>
/// </summary>
internal sealed class SocialReads(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    DashboardAssetUrls assetUrls
) : DashboardReads(dbContextFactory)
{
    private readonly DashboardAssetUrls _assetUrls = assetUrls;

    public Task<SocialStats> SocialStatsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<SocialStats>(
            async db =>
            {
                DateTime until = TimeWindow.ParseDateTime(query["until"]) ?? DateTime.UtcNow;
                DateTime since = TimeWindow.ParseDateTime(query["since"]) ?? until.AddDays(-30);
                string granularity = TimeWindow.Granularity(query["granularity"]);

                int friendRows = await db
                    .MessengerFriends.AsNoTracking()
                    .CountAsync(f => f.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                int playersWithFriends = await db
                    .MessengerFriends.AsNoTracking()
                    .Where(f => f.DeletedAt == null)
                    .Select(f => f.PlayerEntityId)
                    .Distinct()
                    .CountAsync(ct)
                    .ConfigureAwait(false);

                int pendingRequests = await db
                    .MessengerRequests.AsNoTracking()
                    .CountAsync(r => r.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                int blockedPairs = await db
                    .MessengerBlocked.AsNoTracking()
                    .CountAsync(b => b.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                int ignoredPairs = await db
                    .MessengerIgnored.AsNoTracking()
                    .CountAsync(i => i.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                int totalMessages = await db
                    .MessengerMessages.AsNoTracking()
                    .CountAsync(ct)
                    .ConfigureAwait(false);

                int undelivered = await db
                    .MessengerMessages.AsNoTracking()
                    .CountAsync(m => !m.Delivered, ct)
                    .ConfigureAwait(false);

                List<MessageStatsRow> windowMessages = await db
                    .MessengerMessages.AsNoTracking()
                    .Where(m => m.Timestamp >= since && m.Timestamp <= until)
                    .Select(m => new MessageStatsRow(m.Timestamp, m.SenderEntityId))
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

                foreach (MessageStatsRow row in windowMessages)
                {
                    DateTime bucket = TimeWindow.Bucket(row.Timestamp, granularity);
                    bucketMap[bucket] = bucketMap.GetValueOrDefault(bucket) + 1;
                }

                List<SocialTimelinePoint> timeline = bucketMap
                    .OrderBy(pair => pair.Key)
                    .Select(pair => new SocialTimelinePoint(
                        pair.Key.ToString("O"),
                        TimeWindow.Label(pair.Key, granularity),
                        pair.Value
                    ))
                    .ToList();

                var topSenderRows = windowMessages
                    .GroupBy(m => m.SenderId)
                    .Select(g => new { playerId = g.Key, messages = g.Count() })
                    .OrderByDescending(g => g.messages)
                    .Take(10)
                    .ToList();

                var topFriendedRows = await db
                    .MessengerFriends.AsNoTracking()
                    .Where(f => f.DeletedAt == null)
                    .GroupBy(f => f.PlayerEntityId)
                    .Select(g => new { playerId = g.Key, friends = g.Count() })
                    .OrderByDescending(g => g.friends)
                    .Take(10)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> names = await db.PlayerNamesAsync(
                        DisplayNameQueries.NormalizeIds(
                            topSenderRows
                                .Select(s => (int?)s.playerId)
                                .Concat(topFriendedRows.Select(f => (int?)f.playerId))
                        ),
                        ct
                    )
                    .ConfigureAwait(false);

                int threads = await db
                    .GroupForumThreads.AsNoTracking()
                    .CountAsync(t => t.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                int posts = await db
                    .GroupForumPosts.AsNoTracking()
                    .CountAsync(p => p.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                // Grouped by the enum itself and named afterwards: naming the key inside the query
                // would push a ToString() onto a GROUP BY key, which is not reliably translatable.
                var threadStateRows = await db
                    .GroupForumThreads.AsNoTracking()
                    .Where(t => t.DeletedAt == null)
                    .GroupBy(t => t.State)
                    .Select(g => new { state = g.Key, count = g.Count() })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<ForumStateCount> threadsByState = threadStateRows
                    .Select(r => new ForumStateCount(r.state.ToString(), r.count))
                    .ToList();

                var postStateRows = await db
                    .GroupForumPosts.AsNoTracking()
                    .Where(p => p.DeletedAt == null)
                    .GroupBy(p => p.State)
                    .Select(g => new { state = g.Key, count = g.Count() })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<ForumStateCount> postsByState = postStateRows
                    .Select(r => new ForumStateCount(r.state.ToString(), r.count))
                    .ToList();

                var topForumRows = await db
                    .GroupForumThreads.AsNoTracking()
                    .Where(t => t.DeletedAt == null)
                    .GroupBy(t => t.GroupEntityId)
                    .Select(g => new
                    {
                        groupId = g.Key,
                        threads = g.Count(),
                        postCount = g.Sum(t => t.PostCount),
                        lastPostAt = g.Max(t => t.LastPostAt),
                    })
                    .OrderByDescending(g => g.postCount)
                    .Take(10)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<int> groupIds = topForumRows.ConvertAll(g => g.groupId);
                Dictionary<int, (string Name, string? Badge)> groupCards = (
                    await db
                        .Groups.AsNoTracking()
                        .Where(g => groupIds.Contains(g.Id))
                        .Select(g => new
                        {
                            g.Id,
                            g.Name,
                            g.Badge,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false)
                ).ToDictionary(g => g.Id, g => (g.Name, (string?)g.Badge));

                var recentThreads = await db
                    .GroupForumThreads.AsNoTracking()
                    .Where(t => t.DeletedAt == null)
                    .OrderByDescending(t => t.LastPostAt ?? t.CreatedAt)
                    .Take(15)
                    .Select(t => new
                    {
                        t.Id,
                        t.GroupEntityId,
                        t.Subject,
                        state = t.State.ToString(),
                        t.IsPinned,
                        t.PostCount,
                        t.LastPostAt,
                        t.CreatedAt,
                        authorId = t.PlayerEntityId,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<int> recentGroupIds = recentThreads.ConvertAll(t => t.GroupEntityId);
                Dictionary<int, (string Name, string? Badge)> recentGroupCards = (
                    await db
                        .Groups.AsNoTracking()
                        .Where(g => recentGroupIds.Contains(g.Id))
                        .Select(g => new
                        {
                            g.Id,
                            g.Name,
                            g.Badge,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false)
                ).ToDictionary(g => g.Id, g => (g.Name, (string?)g.Badge));

                Dictionary<int, string> authorNames = await db.PlayerNamesAsync(
                        DisplayNameQueries.NormalizeIds(
                            recentThreads.Select(t => (int?)t.authorId)
                        ),
                        ct
                    )
                    .ConfigureAwait(false);

                return new SocialStats(
                    new ReportWindow(since, until, granularity),
                    new SocialTotals(
                        // Both directions are stored, so a friendship is two rows.
                        friendRows / 2,
                        friendRows,
                        playersWithFriends,
                        pendingRequests,
                        blockedPairs,
                        ignoredPairs,
                        totalMessages,
                        undelivered,
                        windowMessages.Count,
                        threads,
                        posts
                    ),
                    timeline,
                    topSenderRows
                        .Select(s => new SocialSenderCount(
                            s.playerId,
                            DisplayNameQueries.ResolvePlayerName(names, s.playerId),
                            s.messages
                        ))
                        .ToList(),
                    topFriendedRows
                        .Select(f => new SocialFriendedCount(
                            f.playerId,
                            DisplayNameQueries.ResolvePlayerName(names, f.playerId),
                            f.friends
                        ))
                        .ToList(),
                    new SocialForums(
                        threadsByState,
                        postsByState,
                        topForumRows
                            .Select(g => new ForumGroupRanking(
                                g.groupId,
                                // A guild deleted out from under its threads is absent here, and
                                // the tuple a miss returns carries nulls, not an exception.
                                groupCards.GetValueOrDefault(g.groupId).Name,
                                _assetUrls.GroupBadge(
                                    groupCards.GetValueOrDefault(g.groupId).Badge
                                ),
                                g.threads,
                                g.postCount,
                                g.lastPostAt
                            ))
                            .ToList(),
                        recentThreads
                            .Select(t => new ForumThreadSummary(
                                t.Id,
                                t.GroupEntityId,
                                recentGroupCards.GetValueOrDefault(t.GroupEntityId).Name,
                                _assetUrls.GroupBadge(
                                    recentGroupCards.GetValueOrDefault(t.GroupEntityId).Badge
                                ),
                                t.Subject,
                                t.state,
                                t.IsPinned,
                                t.PostCount,
                                t.LastPostAt,
                                t.CreatedAt,
                                t.authorId,
                                DisplayNameQueries.ResolvePlayerName(authorNames, t.authorId)
                            ))
                            .ToList()
                    )
                );
            },
            ct
        );

    private sealed record MessageStatsRow(DateTime Timestamp, int SenderId);
}
