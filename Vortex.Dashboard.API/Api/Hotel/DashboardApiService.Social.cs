using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// The social graph and the guild forums: friendships, pending requests, the blocking/ignoring that
/// is invisible from any other surface, private-message volume, and forum activity.
/// <para>
/// <c>messenger_friends</c> holds one row per direction (both are written on accept), so a raw row
/// count double-counts every friendship — the totals here halve it and say so.
/// </para>
/// </summary>
internal sealed partial class DashboardApiService
{
    public Task<object> SocialStatsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                DateTime until = ParseDateTime(query["until"]) ?? DateTime.UtcNow;
                DateTime since = ParseDateTime(query["since"]) ?? until.AddDays(-30);
                string granularity = NormalizeGranularity(query["granularity"]);

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
                DateTime cursor = ResolveCalendarBucket(since, granularity);
                DateTime end = ResolveCalendarBucket(until, granularity);

                while (cursor <= end)
                {
                    bucketMap[cursor] = 0;
                    cursor = NextCalendarBucket(cursor, granularity);
                }

                foreach (MessageStatsRow row in windowMessages)
                {
                    DateTime bucket = ResolveCalendarBucket(row.Timestamp, granularity);
                    bucketMap[bucket] = bucketMap.GetValueOrDefault(bucket) + 1;
                }

                var timeline = bucketMap
                    .OrderBy(pair => pair.Key)
                    .Select(pair => new
                    {
                        bucket = pair.Key.ToString("O"),
                        label = FormatCalendarLabel(pair.Key, granularity),
                        messages = pair.Value,
                    })
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

                Dictionary<int, string> names = await LoadPlayerNamesAsync(
                        db,
                        NormalizeIds(
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

                var threadsByState = threadStateRows
                    .Select(r => new { state = r.state.ToString(), r.count })
                    .ToList();

                var postStateRows = await db
                    .GroupForumPosts.AsNoTracking()
                    .Where(p => p.DeletedAt == null)
                    .GroupBy(p => p.State)
                    .Select(g => new { state = g.Key, count = g.Count() })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var postsByState = postStateRows
                    .Select(r => new { state = r.state.ToString(), r.count })
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

                Dictionary<int, string> authorNames = await LoadPlayerNamesAsync(
                        db,
                        NormalizeIds(recentThreads.Select(t => (int?)t.authorId)),
                        ct
                    )
                    .ConfigureAwait(false);

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
                        // Both directions are stored, so a friendship is two rows.
                        friendships = friendRows / 2,
                        friendRows,
                        playersWithFriends,
                        pendingRequests,
                        blockedPairs,
                        ignoredPairs,
                        totalMessages,
                        undelivered,
                        windowMessages = windowMessages.Count,
                        threads,
                        posts,
                    },
                    timeline,
                    topSenders = topSenderRows
                        .Select(s => new
                        {
                            s.playerId,
                            playerName = ResolvePlayerName(names, s.playerId),
                            s.messages,
                        })
                        .ToList(),
                    topFriended = topFriendedRows
                        .Select(f => new
                        {
                            f.playerId,
                            playerName = ResolvePlayerName(names, f.playerId),
                            f.friends,
                        })
                        .ToList(),
                    forums = new
                    {
                        threadsByState,
                        postsByState,
                        topGroups = topForumRows
                            .Select(g => new
                            {
                                g.groupId,
                                groupName = groupCards.GetValueOrDefault(g.groupId).Name,
                                badgeUrl = _assetUrls.GroupBadge(
                                    groupCards.GetValueOrDefault(g.groupId).Badge
                                ),
                                g.threads,
                                g.postCount,
                                g.lastPostAt,
                            })
                            .ToList(),
                        recentThreads = recentThreads
                            .Select(t => new
                            {
                                t.Id,
                                groupId = t.GroupEntityId,
                                groupName = recentGroupCards
                                    .GetValueOrDefault(t.GroupEntityId)
                                    .Name,
                                badgeUrl = _assetUrls.GroupBadge(
                                    recentGroupCards.GetValueOrDefault(t.GroupEntityId).Badge
                                ),
                                t.Subject,
                                t.state,
                                t.IsPinned,
                                t.PostCount,
                                t.LastPostAt,
                                t.CreatedAt,
                                t.authorId,
                                authorName = ResolvePlayerName(authorNames, t.authorId),
                            })
                            .ToList(),
                    },
                };
            },
            ct
        );

    private sealed record MessageStatsRow(DateTime Timestamp, int SenderId);
}
