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
using Vortex.Database.Entities.Groups;

namespace Vortex.Dashboard.API.Api.Hotel;

/// <summary>
/// What an operator needs to see before acting on a guild or its forum.
/// </summary>
/// <remarks>
/// <para>
/// Separate from <see cref="GroupReads"/>, which answers the read-only analytics page: that one is
/// about the domain in aggregate — how many guilds, growing how fast, which are busiest. This one is
/// about one guild and the four lists somebody is about to act on, and it shows what the players
/// cannot: hidden threads, hidden posts, deleted posts.
/// </para>
/// <para>
/// Every query here is <c>AsNoTracking</c> and reads the tables directly, which is correct for this
/// subject and not a shortcut: <c>GroupGrain</c> and <c>GroupForumGrain</c> hold no state between
/// calls — each opens its own context — so there is no cached copy for a read to be stale against.
/// The writes are the other half of that story and do go through the grains, because the
/// notifications they send are state that only the grains know how to reach.
/// </para>
/// </remarks>
internal sealed class GuildReads(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    DashboardAssetUrls assetUrls
) : DashboardReads(dbContextFactory)
{
    private readonly DashboardAssetUrls _assetUrls = assetUrls;

    /// <summary>How much of a guild is worth listing at once. Past this an operator is looking for
    /// one name, and the filter is the tool for that.</summary>
    private const int ListCap = 300;

    /// <summary>Paginated guild roster. Filters: <c>q</c> (name), <c>ownerId</c>, and
    /// <c>pending=true</c> for the guilds with membership requests waiting.</summary>
    public Task<GuildDirectoryPage> GuildsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<GuildDirectoryPage>(
            async db =>
            {
                string term = (query["q"] ?? string.Empty).Trim();
                int limit = QueryValues.Limit(query["limit"], 40, 200);
                int page = QueryValues.Page(query["page"]);
                int offset = Math.Max(0, (page - 1) * limit);

                IQueryable<GroupEntity> guilds = db
                    .Groups.AsNoTracking()
                    .Where(g => g.DeletedAt == null);

                if (term.Length > 0)
                {
                    guilds = guilds.Where(g => g.Name.Contains(term));
                }

                if (int.TryParse(query["ownerId"], out int ownerId) && ownerId > 0)
                {
                    guilds = guilds.Where(g => g.OwnerPlayerEntityId == ownerId);
                }

                if (bool.TryParse(query["pending"], out bool pending) && pending)
                {
                    guilds = guilds.Where(g =>
                        db.GroupMembershipRequests.Any(r =>
                            r.GroupEntityId == g.Id && r.DeletedAt == null
                        )
                    );
                }

                int total = await guilds.CountAsync(ct).ConfigureAwait(false);

                var rows = await guilds
                    .OrderByDescending(g => g.Id)
                    .Skip(offset)
                    .Take(limit)
                    .Select(g => new
                    {
                        g.Id,
                        g.Name,
                        g.Badge,
                        Type = g.Type.ToString(),
                        g.OwnerPlayerEntityId,
                        g.RoomEntityId,
                        g.CreatedAt,
                        Members = db.GroupMembers.Count(m =>
                            m.GroupEntityId == g.Id && m.DeletedAt == null
                        ),
                        Requests = db.GroupMembershipRequests.Count(r =>
                            r.GroupEntityId == g.Id && r.DeletedAt == null
                        ),
                        Bans = db.GroupBlockedMembers.Count(b =>
                            b.GroupEntityId == g.Id && b.DeletedAt == null
                        ),
                        Threads = db.GroupForumThreads.Count(t =>
                            t.GroupEntityId == g.Id && t.DeletedAt == null
                        ),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> ownerNames = await db.PlayerNamesAsync(
                        DisplayNameQueries.NormalizeIds(
                            rows.Select(g => (int?)g.OwnerPlayerEntityId)
                        ),
                        ct
                    )
                    .ConfigureAwait(false);

                Dictionary<int, string> roomNames = await db.RoomNamesAsync(
                        DisplayNameQueries.NormalizeIds(rows.Select(g => (int?)g.RoomEntityId)),
                        ct
                    )
                    .ConfigureAwait(false);

                List<GuildDirectoryRow> items =
                [
                    .. rows.Select(g => new GuildDirectoryRow(
                        g.Id,
                        g.Name,
                        _assetUrls.GroupBadge(g.Badge),
                        g.Type,
                        g.OwnerPlayerEntityId,
                        DisplayNameQueries.ResolvePlayerName(ownerNames, g.OwnerPlayerEntityId),
                        g.RoomEntityId,
                        roomNames.GetValueOrDefault(g.RoomEntityId),
                        g.Members,
                        g.Requests,
                        g.Bans,
                        g.Threads,
                        g.CreatedAt
                    )),
                ];

                return new GuildDirectoryPage(page, limit, offset, total, items.Count, items);
            },
            ct
        );

    /// <summary>One guild with its members, pending requests, bans and threads. Null if there is no
    /// such guild, or it has already been disbanded.</summary>
    public Task<GuildModeration?> GuildAsync(int guildId, CancellationToken ct) =>
        QueryAsync<GuildModeration?>(
            async db =>
            {
                GroupEntity? guild = await db
                    .Groups.AsNoTracking()
                    .Include(g => g.ForumSettings)
                    .FirstOrDefaultAsync(g => g.Id == guildId && g.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                if (guild is null)
                {
                    return null;
                }

                var members = await db
                    .GroupMembers.AsNoTracking()
                    .Where(m => m.GroupEntityId == guildId && m.DeletedAt == null)
                    .OrderBy(m => m.Id)
                    .Take(ListCap)
                    .Select(m => new
                    {
                        m.PlayerEntityId,
                        Rank = m.Rank.ToString(),
                        m.CreatedAt,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var requests = await db
                    .GroupMembershipRequests.AsNoTracking()
                    .Where(r => r.GroupEntityId == guildId && r.DeletedAt == null)
                    .OrderBy(r => r.Id)
                    .Take(ListCap)
                    .Select(r => new { r.PlayerEntityId, r.CreatedAt })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var bans = await db
                    .GroupBlockedMembers.AsNoTracking()
                    .Where(b => b.GroupEntityId == guildId && b.DeletedAt == null)
                    .OrderBy(b => b.Id)
                    .Take(ListCap)
                    .Select(b => new
                    {
                        b.PlayerEntityId,
                        b.BlockedByPlayerEntityId,
                        b.CreatedAt,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var threads = await db
                    .GroupForumThreads.AsNoTracking()
                    .Where(t => t.GroupEntityId == guildId && t.DeletedAt == null)
                    .OrderByDescending(t => t.LastPostAt ?? t.CreatedAt)
                    .Take(ListCap)
                    .Select(t => new
                    {
                        t.Id,
                        t.Subject,
                        State = t.State.ToString(),
                        t.IsPinned,
                        t.PostCount,
                        t.PlayerEntityId,
                        t.CreatedAt,
                        t.LastPostAt,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<int> playerIds =
                [
                    .. members
                        .Select(m => m.PlayerEntityId)
                        .Concat(requests.Select(r => r.PlayerEntityId))
                        .Concat(bans.Select(b => b.PlayerEntityId))
                        .Concat(bans.Select(b => b.BlockedByPlayerEntityId))
                        .Concat(threads.Select(t => t.PlayerEntityId))
                        .Append(guild.OwnerPlayerEntityId)
                        .Distinct(),
                ];

                Dictionary<int, string> names = await db.PlayerNamesAsync(playerIds, ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> roomNames = await db.RoomNamesAsync(
                        [guild.RoomEntityId],
                        ct
                    )
                    .ConfigureAwait(false);

                int memberCount = await db
                    .GroupMembers.AsNoTracking()
                    .CountAsync(m => m.GroupEntityId == guildId && m.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                GuildIdentity identity = new(
                    guild.Id,
                    guild.Name,
                    guild.Description,
                    _assetUrls.GroupBadge(guild.Badge),
                    guild.Type.ToString(),
                    guild.OwnerPlayerEntityId,
                    DisplayNameQueries.ResolvePlayerName(names, guild.OwnerPlayerEntityId),
                    guild.RoomEntityId,
                    roomNames.GetValueOrDefault(guild.RoomEntityId),
                    guild.ForumSettings is { Enabled: true },
                    memberCount,
                    guild.CreatedAt
                );

                return new GuildModeration(
                    identity,
                    [
                        .. members.Select(m => new GuildMemberRow(
                            m.PlayerEntityId,
                            DisplayNameQueries.ResolvePlayerName(names, m.PlayerEntityId),
                            m.Rank,
                            m.PlayerEntityId == guild.OwnerPlayerEntityId,
                            m.CreatedAt
                        )),
                    ],
                    [
                        .. requests.Select(r => new GuildRequestRow(
                            r.PlayerEntityId,
                            DisplayNameQueries.ResolvePlayerName(names, r.PlayerEntityId),
                            r.CreatedAt
                        )),
                    ],
                    [
                        .. bans.Select(b => new GuildBanRow(
                            b.PlayerEntityId,
                            DisplayNameQueries.ResolvePlayerName(names, b.PlayerEntityId),
                            b.BlockedByPlayerEntityId,
                            DisplayNameQueries.ResolvePlayerName(names, b.BlockedByPlayerEntityId),
                            b.CreatedAt
                        )),
                    ],
                    [
                        .. threads.Select(t => new GuildThreadRow(
                            t.Id,
                            t.Subject,
                            t.State,
                            t.IsPinned,
                            t.PostCount,
                            t.PlayerEntityId,
                            DisplayNameQueries.ResolvePlayerName(names, t.PlayerEntityId),
                            t.CreatedAt,
                            t.LastPostAt
                        )),
                    ]
                );
            },
            ct
        );

    /// <summary>One thread and its posts, including the hidden and the deleted. Null if the thread
    /// does not belong to that guild.</summary>
    public Task<GuildThreadDetail?> ThreadAsync(int guildId, int threadId, CancellationToken ct) =>
        QueryAsync<GuildThreadDetail?>(
            async db =>
            {
                var thread = await db
                    .GroupForumThreads.AsNoTracking()
                    .Where(t =>
                        t.Id == threadId && t.GroupEntityId == guildId && t.DeletedAt == null
                    )
                    .Select(t => new
                    {
                        t.Id,
                        t.Subject,
                        State = t.State.ToString(),
                        t.IsPinned,
                        t.PostCount,
                        t.PlayerEntityId,
                        t.CreatedAt,
                        t.LastPostAt,
                        GuildName = t.GroupEntity.Name,
                    })
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (thread is null)
                {
                    return null;
                }

                // IgnoreQueryFilters, and it is the whole point of this read: every VortexEntity
                // carries a global `DeletedAt == null` filter, so without it a deleted post is
                // invisible here exactly as it is everywhere else -- and the answer to "what did it
                // say before it was removed" would be silently empty rather than refused. No state
                // filter either: a hidden post is the one being asked about.
                var posts = await db
                    .GroupForumPosts.AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(p => p.ThreadEntityId == threadId && p.GroupEntityId == guildId)
                    .OrderBy(p => p.Id)
                    .Take(ListCap)
                    .Select(p => new
                    {
                        p.Id,
                        p.Message,
                        State = p.State.ToString(),
                        p.DeletedAt,
                        p.PlayerEntityId,
                        p.AdminPlayerEntityId,
                        p.CreatedAt,
                        p.AdminOperationAt,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> names = await db.PlayerNamesAsync(
                        DisplayNameQueries.NormalizeIds(
                            posts
                                .Select(p => (int?)p.PlayerEntityId)
                                .Concat(posts.Select(p => p.AdminPlayerEntityId))
                                .Append(thread.PlayerEntityId)
                        ),
                        ct
                    )
                    .ConfigureAwait(false);

                return new GuildThreadDetail(
                    guildId,
                    thread.GuildName,
                    new GuildThreadRow(
                        thread.Id,
                        thread.Subject,
                        thread.State,
                        thread.IsPinned,
                        thread.PostCount,
                        thread.PlayerEntityId,
                        DisplayNameQueries.ResolvePlayerName(names, thread.PlayerEntityId),
                        thread.CreatedAt,
                        thread.LastPostAt
                    ),
                    [
                        .. posts.Select(p => new GuildPostRow(
                            p.Id,
                            threadId,
                            p.Message,
                            p.State,
                            p.DeletedAt is not null,
                            p.PlayerEntityId,
                            DisplayNameQueries.ResolvePlayerName(names, p.PlayerEntityId),
                            p.AdminPlayerEntityId,
                            DisplayNameQueries.ResolvePlayerName(names, p.AdminPlayerEntityId),
                            p.CreatedAt,
                            p.AdminOperationAt
                        )),
                    ]
                );
            },
            ct
        );
}
